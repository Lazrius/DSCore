/**
 * DXT decompression routines
 *
 * While WebGL has extension WEBGL_compressed_texture_s3tc to load DXT
 * compressed textures the vast majority of mobile devices do not support it.
 *
 * Additionally gl.TEXTURE_MAX_LEVEL is available only to WebGL2RenderingContext
 * while DXT textures don't go below 4x4 as this is the size of compression
 * block and therefore minimum texture size, which in turn causes errors with
 * gl.TEXTURE_MIN_FILTER set to gl.LINEAR_MIPMAP_LINEAR. Furthermore missing
 * mipmaps cannot be generated automatically via gl.generateMipmap() as it's not
 * supported for compressed textures, and I'd rather use mipmaps than not.
 *
 * WebGL2RenderingContext itself isn't widely avaiable on mobile devices either,
 * compounding problem even further with Freelancer assets. Decompressing
 * textures into raw RGB(A) 888(8) buffer and running gl.generateMipmap() over
 * to cover for missing mipmaps solves all these problems. The cost of
 * decompressing on-the-fly isn't substantial in this particular case because
 * textures in Freelancer are typicaly limited to 256x256 and there aren't many
 * of them either, it's just easier to decompress for plain gl.RGB/gl.RGBA +
 * gl.UNSIGNED_BYTE and not having to worry about limited platform support. Were
 * textures of high resultion and each material used several layers then perhaps
 * it would have made necessary to take a different approach.
 */
import {types} from "./texture.js";

/**
 * Convert 16-bit integer (5-6-5) pixel to 24-bit (8-8-8)
 * @param {Number} pixel  Uint16 pixel data as 5-6-5
 * @return {Array} RGB channels or whatever else would be packed into 5-6-5 bits
 */
function from565to888(pixel) {
	let r = (pixel >> 11) & 0x1F,
		g = (pixel >> 5)  & 0x3F,
		b = pixel & 0x1f;

	r = (r << 3) | (r >> 2);
	g = (g << 2) | (g >> 4);
	b = (b << 3) | (b >> 2);

	return [r, g, b];
}

/**
 * Decompress RGB channels.
 *
 * @param {Uint8Array} target 4x4 pixel block
 * @param {Uint8Array} source Compressed data block
 * @param {Number}     offset Offset to compressed data
 */
function decompressRGB(target, source, offset){
	const colors = new Uint8Array(16), // Block colors
		indices = new Uint8Array(16); // Block indices

	// Pick two colors, set alpha flag and target buffer number of channels
	let a = source[offset + 1] << 8 | source[offset + 0],
		b = source[offset + 3] << 8 | source[offset + 2],
		c = 0, d = 0, alpha = a <= b, channels = target.byteLength / 16;

	// Unpack 565 colors into color table
	[colors[0], colors[1], colors[2]] = from565to888(a);
	[colors[4], colors[5], colors[6]] = from565to888(b);
	colors[3] = colors[7] = 0xFF;

	// Decode RGB into color table
	for (let i = 0; i < 3; i++) {
		c = colors[i];
		d = colors[4 + i];

		colors[i + 8]  = alpha ? (c + d) / 2 : (2 * c + d) / 3;
		colors[i + 12] = alpha ? 0           : (c + 2 * d) / 3;
	}

	// Decode alpha into color table
	colors[8  + 3] = 0xFF;
	colors[12 + 3] = alpha ? 0 : 0xFF;

	// Unpack to indices
	for (let i = 0, p = 0; i < 4; i++) {
		p = source[offset + i + 4];

		indices[i * 4 + 0] = p & 0x3;
		indices[i * 4 + 1] = (p >> 2) & 0x3;
		indices[i * 4 + 2] = (p >> 4) & 0x3;
		indices[i * 4 + 3] = (p >> 6) & 0x3;
	}

	// Write color channels to output buffer
	for (let i = 0, m = 0, o = 0; i < indices.length; i++) {
		o = 4 * indices[i];

		target[m++] = colors[o + 0]; // Red
		target[m++] = colors[o + 1]; // Green
		target[m++] = colors[o + 2]; // Blue
		if (channels == 4) target[m++] = colors[o + 3]; // Alpha
	}

	return true;
}

/**
 * Decompress DXT3 alpha channel
 * @param  {Uint8Array} target 4x4 pixel block
 * @param  {Uint8Array} source Compressed data block
 * @param  {Number}     offset Offset to compressed data
 * @return {Boolean}
 */
function decompressAlphaDXT3(target, source, offset){
	for (let i = 0, q = 0, l = 0, h = 0; i < 8; i++) {
		q = source[offset + i];
		l = q & 0x0F;
		h = q & 0xF0;

		// Override alpha in target pixel block
		target[i * 8 + 3] = l | ( l << 4 );
		target[i * 8 + 7] = h | ( h >> 4 );
	}

	return true;
}

/**
 * Decompress DXT5 alpha channel
 * @param  {Uint8Array} target 4x4 pixel block
 * @param  {Uint8Array} source Compressed data block
 * @param  {Number}     offset Offset to compressed data
 * @return {Boolean}
 */
function decompressAlphaDXT5(target, source, offset){
	const alphas = new Uint8Array(8), // Alpha table
		indices  = new Uint8Array(16),
		alpha0   = source[offset + 0],
		alpha1   = source[offset + 1];

	alphas[0] = alpha0;
	alphas[1] = alpha1;
	alphas[6] = 0x00; // 5 point alpha min
	alphas[7] = 0xFF; // 5 point alpha max

	// Decode 5/7-point alphas
	if (alpha0 > alpha1) for (let i = 1; i < 7; i++) alphas[i + 1] = ((7 - i) * alpha0 + i * alpha1) / 7;
	else for (let i = 1; i < 5; i++) alphas[i + 1] = ((5 - i) * alpha0 + i * alpha1) / 5;

	offset += 2;

	// Pair of three bytes
	for (let i = 0, s = 0, a = 0; i < 2; i++) {
		for (let k = 0; k < 3; k++) a |= (source[offset++] << 8 * k);
		for (let k = 0; k < 8; k++) indices[s++] = (a >> 3 * k) & 0x7;
		a = 0; // Reset
	}

	// Override alpha in target pixel block
	for (let i = 0; i < indices.length; i++) target[i * 4 + 3] = alphas[indices[i]];

	return true;
}

/**
 * DXT1/3/5 compression algorithm operates on 4x4 pixel block. Each compressed
 * data block is decompressed individually and mapped to target RGB(A) pixel
 * buffer.
 * 
 * @param {Uint8Array} source Compressed data buffer
 * @param {Number}     width  Image width
 * @param {Number}     height Image height
 * @param {Number}     type   Compression method (DXT1, DXT3 or DXT5)
 * @param {Boolean}    alpha  Add alpha channel
 * @return {Uint8Array} Uncompressed image buffer
 */
export function decompressDXT(source, width, height, type, alpha) {
	if (! (source instanceof Uint8Array || source instanceof Uint8ClampedArray)) throw new TypeError("Invalid source image type");
	if (width  % 4 != 0) throw new RangeError("Invalid image width");
	if (height % 4 != 0) throw new RangeError("Invalid image height");

	const colorLength = alpha ? 4 : 3,                                // Bytes per color
		strideLength  = width * colorLength,                          // Bytes per image line
		blockLength   = type == types.S3TC_DXT1 ? 8 : 16,             // Compressed block length
		lineLength    = 4 * colorLength,                              // Bytes per pixel block line
		pixels        = new Uint8Array(16 * colorLength),             // Decompressed 4x4 pixel block
		target        = new Uint8Array(width * height * colorLength); // Decompressed image

	// Loop through each block in compressed data buffer
	for (let blockOffset = 0, targetOffset = 0; blockOffset < source.byteLength; blockOffset += blockLength) {
		switch (type) {
			case types.S3TC_DXT1: decompressRGB(pixels, source, blockOffset); break;
			case types.S3TC_DXT3: decompressRGB(pixels, source, blockOffset + 8); if (alpha) decompressAlphaDXT3(pixels, source, blockOffset); break;
			case types.S3TC_DXT5: decompressRGB(pixels, source, blockOffset + 8); if (alpha) decompressAlphaDXT5(pixels, source, blockOffset); break;
			default: throw new TypeError("Invalid DXT compression type");
		}

		// Copy decompressed block buffer to target buffer
		for (let lineOffset = 0, pixelOffset = targetOffset; lineOffset < pixels.byteLength; lineOffset += lineLength) {
			target.set(pixels.subarray(lineOffset, lineOffset + lineLength), pixelOffset);
			pixelOffset += strideLength;
		}

		// Skip three lines for next line of blocks
		if (((targetOffset += lineLength) % strideLength) == 0) targetOffset += strideLength * 3;
	}

	return target;
}