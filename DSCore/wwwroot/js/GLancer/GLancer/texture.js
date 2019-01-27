/**
 * Textures in Freelancer are either uncompressed Targa (16/24/32, as well as
 * indexed palettes) and DirectDraw Surface (compressed DXT1/3/5 as well as
 * RGB-565 and few other uncompressed variants).
 *
 * WebGL does not support swizzle masks to swap color channels so Targa images
 * have their buffers rebuilt.
 *
 * DXT compression typically isn't available in mobile envrionment so compressed
 * textures are decompressed upon loading into buffers.
 */
import {ArrayBufferWalker} from "./core.js";
import {UTFEntry} from "./utf.js";

// Texture mipmap data formats
export const types = Object.create(null);

// Uncompressed texture types:
types.RGB16_565   = 0; // gl.RGB  | gl.UNSIGNED_SHORT_5_6_5
types.RGBA16_4444 = 1; // gl.RGBA | gl.UNSIGNED_SHORT_4_4_4_4
types.RGBA16_5551 = 2; // gl.RGBA | gl.UNSIGNED_SHORT_5_5_5_1
types.RGB24_888   = 3; // gl.RGB  | gl.UNSIGNED_BYTE
types.RGBA32_8888 = 4; // gl.RGBA | gl.UNSIGNED_BYTE

// Compressed texture types:
types.S3TC_DXT1   = 5; // s3tc.COMPRESSED_RGB_S3TC_DXT1_EXT / s3tc.COMPRESSED_RGBA_S3TC_DXT1_EXT
types.S3TC_DXT3   = 7; // s3tc.COMPRESSED_RGBA_S3TC_DXT3_EXT
types.S3TC_DXT5   = 8; // s3tc.COMPRESSED_RGBA_S3TC_DXT5_EXT

// DirectDrawSurface
const DDS_SIGNATURE   = 0x20534444,
	DDS_RESERVED      = 11 * Uint32Array.BYTES_PER_ELEMENT,
	DDS_PIXEL_FORMAT  = 0x1000,
	DDS_MIPMAP_COUNT  = 0x20000,
	DDS_PIXELS_ALPHA  = 0x1,
	DDS_PIXELS_FOURCC = 0x4,
	DDS_PIXELS_RGB    = 0x40,
	DDS_FOURCC_DXT1   = 0x31545844,
	DDS_FOURCC_DXT3   = 0x33545844,
	DDS_FOURCC_DXT5   = 0x35545844;

// Targa 555 conversion channel masks
const TARGA_555_MASK = [0x7C00, 0x3E0, 0x1F];

const TAG_MIP = /^mip(\d)$/i,
	TAG_MIPS  = "mips",
	TAG_CUBE  = "cube";

/**
 * Expands 16-bit bitmap to 24 or 32-bit bitmap with custom color masks
 * 
 * @param  {Uint16Array} source 16bpp pixel array
 * @param  {Number}      mR     Red bit mask
 * @param  {Number}      mG     Green bit mask
 * @param  {Number}      mB     Blue bit mask
 * @param  {Number}      mA     Alpha bit mask
 * @return {Uint8Array}         24/32-bit bitmap
 */
function fromInt16to8888(source, width, height, mR, mG, mB, mA = 0) {
	if (! (source instanceof Uint16Array)) throw new TypeError("Invalid 16bpp source pixel array type");
	if (width * height != source.length) throw new RangeError("Invalid source array size");

	let result = new Uint8Array(width * height * (mA > 0 ? 4 : 3)),
		sR, sG, sB, sA, xR, xG, xB, xA;

	// https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightParallel
	function getShift(m) {
		let c = 16;

		m = m & -m;
		if (m) c--;
		if (m & 0x00FF) c -= 8;
		if (m & 0x0F0F) c -= 4;
		if (m & 0x3333) c -= 2;
		if (m & 0x5555) c -= 1;
		return c;
	}

	// Shifts (s0) and color amp multipliers (x0)
	if (mR) xR = 0xFF / (mR >> (sR = getShift(mR)));
	if (mG) xG = 0xFF / (mG >> (sG = getShift(mG)));
	if (mB) xB = 0xFF / (mB >> (sB = getShift(mB)));
	if (mA) xA = 0xFF / (mA >> (sA = getShift(mA)));

	// Loop over 16-bit pixels
	for (let s = 0, d = 0, p; s < source.length; s++) {
		p = source[s];

		result[d++] = xR * (p & mR) >> sR;
		result[d++] = xG * (p & mG) >> sG;
		result[d++] = xB * (p & mB) >> sB;

		if (mA > 0) result[d++] = xA * (p & mA) >> sA;
	}

	return result;
}

/**
 * Targa format bitmap image
 * 
 * @property {Number}     height
 * @property {Number}     width
 * @property {Number}     depth  Bit depth (24 or 32)
 * @property {Uint8Array} bitmap
 */
export class TargaImage {

	/**
	 * @param {ArrayBufferWalker} data
	 */
	constructor(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object type");

		let header         = data.readView(18),
			textLength     = header.getUint8(0),
			indexedPalette = header.getUint8(1),
			imageType      = header.getUint8(2), // datatypecode: 0 - no image, 1 - indexed, 2 - rgb
			paletteStart   = header.getUint16(3, true), // Index of first entry in color map
			paletteCount   = header.getUint16(5, true), // How many entries are in color map
			paletteDepth   = header.getUint8(7), // How many bits per color map entry
			originX        = header.getUint16(8, true),
			originY        = header.getUint16(10, true),
			width          = header.getUint16(12, true),
			height         = header.getUint16(14, true),
			depth          = header.getUint8(16),
			descriptor     = header.getUint8(17),
			pixelCount     = width * height,
			pixelDepth     = imageType == 1 && indexedPalette ? paletteDepth : depth,
			sourceCount    = imageType == 1 ? paletteCount : pixelCount,
			bitmap; // Resulting RGB(A) pixel buffer

		data.skip(textLength); // Skip ID section
		
		let source = pixelDepth == 16 ? data.readInt16(sourceCount) : data.readInt8(sourceCount * pixelDepth / 8),
			indices;

		// Must be non-indexed colors for internalformat:
		// UNSIGNED_BYTE (Uint8Array) or UNSIGNED_SHORT_5_5_5_1 (Uint16Array)
		if (imageType == 1) indices = data.readInt8(pixelCount * depth / 8);
		else if (imageType != 2) throw new TypeError("Unsupported targa image type");

		// Extend 16-bit (5-5-5-1, last bit is unused, not transparency) to 24-bit	
		if (source instanceof Uint16Array) { // 16-bit colors
			bitmap     = fromInt16to8888(source, width, height, ...TARGA_555_MASK);
			pixelDepth = 24;
		} else if (source instanceof Uint8Array) { // 32-bit colors. Converting BGRA to RGBA
			bitmap = new Uint8Array(pixelCount * pixelDepth / 8);

			for (let p = 0, i = 0, o; p < pixelCount; p++) {
				o = (indices ? indices[p] : p) * pixelDepth / 8;

				bitmap[i++] = source[o + 2]; // Red
				bitmap[i++] = source[o + 1]; // Green
				bitmap[i++] = source[o];     // Blue

				if (pixelDepth == 32) bitmap[i++] = source[o + 3];
			}
		}

		this.width  = width;
		this.height = height;
		this.depth  = pixelDepth;
		this.bitmap = bitmap;
	}
}

/**
 * Microsoft DirectDraw Surface container format.
 * 
 * May have either compressed or uncompressed mipmaps. Freelancer supports only
 * DXT1/3/5 compressed images, and several bitMask modes of uncompressed images.
 */
export class DirectDrawSurface {
	constructor(data) {
		let pixelData = data.clone();

		// Read DDS header
		let [signature, mipmapOffset, flags, height, width, pitch, depth, mipmapCount] = data.readInt32(8);

		if (signature != DDS_SIGNATURE) throw new TypeError("Invalid DDS header");
		if (! (flags & DDS_PIXEL_FORMAT)) throw new TypeError("DDS is missing pixel format header");
		if (! (flags & DDS_MIPMAP_COUNT)) mipmapCount = 1; // DDSD_MIPMAPCOUNT flag, if absent expect single image

		this.flags    = flags;
		this.height   = height;
		this.width    = width;
		this.pitch    = pitch;
		this.depth    = depth;
		this.fourCC   = undefined;
		this.bitCount = 0;
		this.masks    = undefined;

		pixelData.skip(mipmapOffset + Uint32Array.BYTES_PER_ELEMENT);
		data.skip(DDS_RESERVED);

		// Read DDS pixel header
		let [pixelSize, pixelFlags, fourCC, bitCount, maskRed, maskGreen, maskBlue, maskAlpha] = data.readInt32(8);
			
		this.pixelFlags = pixelFlags;

		if (pixelFlags & DDS_PIXELS_FOURCC) this.fourCC = fourCC;
		else if (pixelFlags & DDS_PIXELS_RGB) {
			this.bitCount = bitCount;
			this.masks    = [maskRed, maskGreen, maskBlue, maskAlpha];
		}

		// TODO: Read caps to find cubemap

		// Read mipmaps
		this.mipmaps = [];

		for (let i = 0; i < mipmapCount; i++) {
			let mipmap;

			switch (true) {
				case (this.bitCount == 8):  mipmap = pixelData.readInt8(width * height); break;
				case (this.bitCount == 16): mipmap = pixelData.readInt16(width * height); break;
				case (this.bitCount == 24): mipmap = pixelData.readInt8(width * height * 3); break;
				case (this.bitCount == 32): mipmap = pixelData.readInt8(width * height * 4); break;
				case (this.fourCC == DDS_FOURCC_DXT1): mipmap = pixelData.readInt8((width / 4 * height / 4) * 8); break;
				case (this.fourCC == DDS_FOURCC_DXT3):
				case (this.fourCC == DDS_FOURCC_DXT5): mipmap = pixelData.readInt8((width / 4 * height / 4) * 16);
			}

			this.mipmaps[i] = mipmap;

			width  >>= 1;
			height >>= 1;
		}
	}
}

/**
 * Texture containing one or more mipmaps
 *
 * @property {Number}  height  Level 0 height
 * @property {Number}  width   Level 0 width
 * @property {Number}  depth   Bit depth
 * @property {Number}  type    Mipmap pixel data type
 * @property {Boolean} alpha   Transparency
 * @property {Array}   mipmaps Individual bitmaps
 */
export class Texture {
	constructor() {
		this.height  = 1;
		this.width   = 1;
		this.depth   = 24;
		this.alpha   = false;
		this.type    = types.RGB24_888; // Pixel type
		this.mipmaps = [new Uint8Array(3)];

		this.mipmaps[0].fill(1);

		this.blocklength = 0; // Compressed block length
		this.blockSize   = 0; // Compressed block size (pixels)
	}

	/**
	 * List texture mipmap levels
	 * 
	 * @yield {[Number, Number, Number, TypedArray]} [level, width, height, pixels]
	 */
	*getMipmaps() {
		let width  = this.width,
			height = this.height,
			level  = 0,
			mipmap;

		while (width > 0 && height > 0) {
			mipmap = this.mipmaps[level];

			if (! mipmap) return;
			yield [level, width, height, mipmap];

			level++;
			width  >>= 1;
			height >>= 1;
		}
	}

	/**
	 * Create uncompressed texture from series of targa images each representing a single mipmap
	 * Order of images must match order of mipmaps, each progressively half the size the previous
	 * 
	 * @param  {...TargaImage} images
	 * @return {UncompressedTexture}
	 */
	static fromTarga(...images) {
		if (! images.length) throw new RangeError("No targa images are specified");

		let texture = new this(), width, height;

		texture.mipmaps = [];

		for (let i = 0; i < images.length; i++) {
			let image = images[i];

			if (! (image instanceof TargaImage)) throw new TypeError("Invalid Targa image object type");

			// First level sets texture parameters
			if (i == 0) {
				texture.width  = width  = image.width;
				texture.height = height = image.height;
				texture.depth  = image.depth;

				switch (image.depth) {
					case 24: texture.type = types.RGB24_888; break;
					case 32: texture.type = types.RGBA32_8888; break;
				}
			} else if (image.width != width || image.height != height) throw new RangeError("Invalid mipmap (" + i + ") image resolution");

			// Assign bitmap data to mipmap
			texture.mipmaps[i] = image.bitmap;

			width  >>= 1;
			height >>= 1;
		}

		return texture;
	}

	/**
	 * Create uncompressed texture from DirectDraw Surface image
	 * @param  {DirectDrawSurface} image
	 * @return {UncompressedTexture}
	 */
	static fromDDS(image) {
		if (! (image instanceof DirectDrawSurface)) throw new TypeError("Invalid DirectDraw Surface image object type");

		let texture = new this();

		if (image.bitCount) { // Uncompressed data
			let [red, green, blue, alpha] = image.masks;

			// Validate bit depth and color mask modes
			switch (image.bitCount) {
				case 16:
					if (red == 0xF800 && green == 0x7E0 && blue == 0x1F && alpha == 0) texture.type = types.RGB16_565;
					else if (red == 0xF00 && green == 0xF0 && blue == 0xF && alpha == 0xF000 ) texture.type = types.RGBA16_4444;
					else if (red == 0x7C00 && green == 0x3E0 && blue == 0x1F && alpha == 0x8000) texture.type = types.RGBA16_5551;
					else throw new RangeError("Unsupported 16-bit color mask");

					if (alpha > 0) texture.alpha = true;

					break;
				case 24:
					if (red == 0xFF0000 && green == 0xFF00 && blue == 0xFF && alpha == 0) texture.type = types.RGB24_888;
					else throw new RangeError("Unsupported 24-bit color mask");

					break;
				case 32:
					if (red == 0xFF0000 && green == 0xFF00 && blue == 0xFF && alpha == 0xFF000000) texture.type = types.RGBA32_8888;
					else throw new RangeError("Unsupported 32-bit color mask");

					texture.alpha = true;

					break;
				default: throw new RangeError("Unsupported uncompressed bit depth");
			}

			texture.depth = image.bitCount;
		} else if (image.fourCC) { // Compressed data
			switch (image.fourCC) {
				case DDS_FOURCC_DXT1:
					texture.blockSize   = 4;
					texture.blocklength = 8;
					texture.alpha       = (image.pixelFlags & DDS_PIXELS_ALPHA) > 0;
					texture.type        = texture.alpha ? types.S3TC_DXT1A : types.S3TC_DXT1;

					break;
				case DDS_FOURCC_DXT3:
					texture.blockSize   = 4;
					texture.blocklength = 16;
					texture.alpha       = true;
					texture.type        = types.S3TC_DXT3;

					break;
				case DDS_FOURCC_DXT5:
					texture.blockSize   = 4;
					texture.blocklength = 16;
					texture.alpha       = true;
					texture.type        = types.S3TC_DXT5;

					break;
				default: throw new RangeError("Unsupported compression method");
			}

			texture.depth = texture.alpha ? 32 : 24;
		}

		texture.width   = image.width;
		texture.height  = image.height;
		texture.mipmaps = image.mipmaps;
		return texture;
	}
}

/**
 * Build Texture object from Targa images
 * @param  {UTFEntry} folder
 * @return {Texture}
 */
function loadTargaTexture(folder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object type");

	let match, levels = [];

	for (let [tag, entry] of folder)
		if (entry.hasData && (match = TAG_MIP.exec(tag)))
			levels[parseInt(match[1])] = new TargaImage(entry.data);

	return levels.length ? Texture.fromTarga(...levels) : false;
}

/**
 * Load texture from UTF
 * @param  {UTFEntry} folder
 * @return {Texture}
 */
export function loadTexture(folder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid texture UTF entry object type");

	let [mips, cube] = folder.find(TAG_MIPS, TAG_CUBE),
		texture;

	if (mips) texture = Texture.fromDDS(new DirectDrawSurface(mips.data));
	else if (cube) console.warn("Cubemaps not implemented");
	else texture = loadTargaTexture(folder);

	return texture;
}