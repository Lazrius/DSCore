/**
 * Common utility functions and classes
 */

/**
 * Read blob chunk
 * @param  {Blob}   data
 * @param  {Number} byteOffset
 * @param  {Number} byteLength
 * @return {Promise}
 */
export function readBlob(data, byteOffset = 0, byteLength = data.size - byteOffset) {
	if (! (data instanceof Blob)) throw new TypeError("Invalid binary object type");
	if ((byteOffset + byteLength) > data.size) throw new RangeError("Chunk block is out of range");

	const reader = new FileReader();

	return new Promise((resolve, reject) => {
		reader.onload  = event => resolve(event.target.result);
		reader.onerror = event => reject(event);
		reader.readAsArrayBuffer(data.slice(byteOffset, byteOffset + byteLength));
	});
}

/**
 * Concatenate multiple ArrayBuffer or TypedArray objects
 * Quite surprising JavaScript typed arrays doesn't have this functionality
 * @param   {ArrayBuffer|TypedArray} buffers Typed arrays and buffers to merge
 * @returns {ArrayBuffer} Concatenated buffer
 */
export function concatBuffers(...buffers) {
	let size = 0, offset = 0;

	return buffers.filter(source => source instanceof ArrayBuffer || ArrayBuffer.isView(source) ? size += source.byteLength : false).reduce((target, source) => {
		target.set(ArrayBuffer.isView(source) ? new Uint8Array(source.buffer, source.byteOffset, source.byteLength) : new Uint8Array(source), offset);
		offset += source.byteLength;
		return target;
	}, new Uint8Array(size)).buffer;
}

/**
 * Get index with consequtive ranges list. Used to find matching LOD level.
 * @param  {Number} distance Number to find index for
 * @param  {Array}  ranges   Array of numbers, each next number greater than previous
 * @param  {Number} bias     Bias multiplier
 * @return {Number}
 */
export function getRangeIndex(distance = 0, ranges, bias = 1) {
	if (! Array.isArray(ranges) || ranges.length < 2) return 0;

	let level = 0, min, max;

	while (level < ranges.length && ranges[level + 1]) {
		min = ranges[level] * bias;
		max = ranges[level + 1] * bias;

		if (min >= max) return 0; // Should not occur
		if (distance >= min && distance < max) return level;
		level++;
	}

	return level;
}

/**
 * Custom ArrayBuffer walker
 * 
 * @property {ArrayBuffer} buffer     Target buffer
 * @property {Number}      byteOffset Current byte offset
 * @property {Number}      byteLength Remaining byte length to read
 */
export class ArrayBufferWalker {

	/**
	 * @param  {ArrayBuffer} buffer      Typically UTF reader data buffer
	 * @param  {Number}      startOffset Entry data start offset
	 * @param  {Number}      endOffset   Entry data end offset
	 * @param  {Number}      offset      Reader pointer local offset
	 */
	constructor(buffer, byteOffset = 0, byteLength = 0) {
		if (! (buffer instanceof ArrayBuffer)) throw new TypeError("Invalid array buffer object type");
		if (! (Number.isInteger(byteOffset) && byteOffset >= 0)) throw new RangeError("Invalid byte offset");
		if (! (Number.isInteger(byteLength) && byteOffset + byteLength <= buffer.byteLength)) throw new RangeError("Invalid byte length");
		if (! byteLength) byteLength = buffer.byteLength - byteOffset;

		this.buffer     = buffer;
		this.byteOffset = byteOffset;
		this.byteLength = byteLength;
	}

	/**
	 * Create ArrayBufferWalker from TypedArray
	 * @param  {TypedArray} view       TypedArray or DataView object
	 * @param  {Number}     byteOffset Offset within view object
	 * @param  {Number}     byteLength Length
	 * @return {ArrayBufferWalker}
	 */
	static from(view, byteOffset = 0, byteLength = 0) {
		if (! ArrayBuffer.isView(view)) throw new TypeError("Invalid view object type");
		if (! (Number.isInteger(byteLength) && byteOffset + byteLength <= view.byteLength)) throw new RangeError("Invalid byte length");
		if (! byteLength) byteLength = view.byteLength - byteOffset;

		return new this(view.buffer, view.byteOffset + byteOffset, byteLength);
	}

	/**
	 * Spawn new data reader with current offset and length
	 * @return {ArrayBufferWalker}
	 */
	clone() {
		return new this.constructor(this.buffer, this.byteOffset, this.byteLength);
	}

	/**
	 * Move curcor by length
	 * @param  {[type]} length [description]
	 * @return {[type]}        [description]
	 */
	move(length) {
		this.byteOffset += length;
		this.byteLength -= length;
	}

	/**
	 * Skip bytes in reader
	 * @param  {Number} length Number of bytes to skip
	 * @return {Boolean}
	 */
	skip(length) {
		if (! (Number.isInteger(length) && length >= 0)) return false;
		if (length > this.byteLength) throw new RangeError("Exceeds remaining bytes");

		this.move(length);
		return true;
	}

	/**
	 * Copy buffer part
	 * @param  {Number} length Byte length
	 * @return {ArrayBuffer}
	 */
	copy(length) {
		if (! length) length = this.byteLength;
		else if (length > this.byteLength) throw new RangeError("Exceeds remaining bytes");

		let result = this.buffer.slice(this.byteOffset, this.byteOffset + length);

		this.move(length);
		return result;
	}

	/**
	 * Read ASCIIZ string from current offset
	 * By default takes all length availble to arraybuffer walker
	 * Reads string till NUL character or to length
	 * @param  {Number} length Bytes to read
	 * @return {String}
	 */
	readString(length = 0) {
		if (! length) length = this.byteLength;
		else if (length > this.byteLength) throw new RangeError("Exceeds remaining bytes");

		let bytes     = new Uint8Array(this.buffer, this.byteOffset, length),
			endOffset = bytes.indexOf(0);

		this.move(length);
		return String.fromCharCode(...(endOffset >= 0 ? bytes.subarray(0, endOffset) : bytes));
	}

	/**
	 * Read elements in entry data buffer as typed array
	 * @param  {function} typedArray
	 * @param  {Number} elements
	 * @return {TypedArray}
	 */
	readTypedArray(typedArray, elements = 0) {
		if (! (Number.isInteger(elements) && elements >= 0)) throw new RangeError("Invalid element count");
		else if (! elements) elements = Math.floor(this.byteLength / typedArray.BYTES_PER_ELEMENT);

		let result, length = typedArray.BYTES_PER_ELEMENT * elements;

		if (length > this.byteLength) throw new RangeError("Exceeds remaining bytes");
		result = new typedArray(this.buffer, this.byteOffset, elements);

		this.move(length);
		return result;
	}

	/**
	 * Read bytes into DataView object
	 * @param  {Number} bytes
	 * @return {DataView}
	 */
	readView(length) {
		if (! length) length = this.byteLength;
		else if (length > this.byteLength) throw new RangeError("Exceeds remaining bytes");

		let result = new DataView(this.buffer, this.byteOffset, length);
		
		this.move(length);
		return result;
	}

	readInt8(length = 0, signed = false) { return this.readTypedArray(signed ? Int8Array : Uint8Array, length); }
	readInt16(elements = 0, signed = false) { return this.readTypedArray(signed ? Int16Array : Uint16Array, elements); }
	readInt32(elements = 0, signed = false) { return this.readTypedArray(signed ? Int32Array : Uint32Array, elements); }
	readFloat32(elements = 0) { return this.readTypedArray(Float32Array, elements); }

	readVector2D() { return this.readFloat32(2); }
	readVector3D() { return this.readFloat32(3); }
	readVector4D() { return this.readFloat32(4); }
	readMatrix3x3() { return this.readFloat32(9); }
	readMatrix4x3() { return this.readFloat32(12); }
	readMatrix4x4() { return this.readFloat32(16); }
}

/**
 * Engine loop time watch
 * 
 * @property {Number} ticks    Ticks counter
 * @property {Number} delta    Difference between now and before
 * @property {Number} previous Previous tick timestamp
 * @property {Number} elapsed  Time elapsed since first tick
 */
export class TimeWatch {
	constructor() {
		this.ticks    = 0;
		this.delta    = 0;
		this.previous = performance.now();
		this.elapsed  = 0;
	}

	/**
	 * Tick
	 * @param  {DOMHighResTimeStamp} time Timestamp from performance.now()
	 * @return {Boolean}
	 */
	tick(time = 0) {
		this.elapsed += this.delta = time - this.previous;
		this.previous = time;
		this.ticks++;
		return true;
	}
}