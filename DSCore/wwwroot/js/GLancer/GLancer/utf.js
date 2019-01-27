/**
 * Most data in Freelancer is stored in UTF files. Presumably stands for
 * "Universal Tree Format" it is a poor man's FAT filesystem in a file with
 * hierarchy of folders and files.
 *
 * Each UTF has single root entry with the name "\". Technically it can have a
 * sibling, therefore having other entries on the same level as root entry, but
 * it's never used in game files and presumed to be invalid state.
 *
 * Other than fixed header everything else is referenced by offsets, global or
 * local to scope, making the structure flexible but also prone to errors.
 *
 * Folder names should be within ASCII range as most existing tools do not
 * support multibyte encoding.
 */
/**
 * UTF header (0x38 bytes, uInt32)
 * -----------------------------------------------------------------------------
 * signature             FourCC bytes ("UTF ")
 * version               File version number (0x101)
 * treeOffset            Offset to tree root (bytes, from file start)
 * treeSize              Size of tree block (bytes)
 * unusedOffset          Cutoff block offset (not important)
 * entrySize             Size of a tree entry (bytes) (must be 44)
 * namesOffset           Offset to entry filenames (bytes, from file start)
 * namesSize             Allocated size of filenames block (bytes)
 * namesUsed             Used size of filenames block (bytes)
 * dataOffset            Offset to data block (bytes, from file start)
 * filetimeLow
 * filetimeHigh
 *
 * UTF entry (0x2C bytes, uInt32)
 * -----------------------------------------------------------------------------
 * nextOffset            Offset to next sibling relative to names offset
 * nameOffset            Offset to entry name relative to tree offset
 * fileAttributes        Entry properties determining whether it is a file or a
 *                       folder (see: Win32 API dwFileAttributes)
 * sharingAttributes     Unused, bitmask for file sharing attributes
 * childOffset           Offset either to first child from tree offset or file
 *                       contents from data offset
 * dataSizeAllocated     Allocated length in data block to entry value
 * dataSizeUsed          Actual used space, less or equal to allocated
 * dataSizeUncompressed  Unused in game, typically the same as used space
 * createTimestamp       Timestamps
 * modifyTimestamp      
 * accessTimestamp
 */
import {readBlob, ArrayBufferWalker} from "./core.js";

const UTF_SIGNATURE = 0x20465455,
	UTF_VERSION     = 0x101,
	UTF_HEADER_SIZE = 0x38,
	UTF_ENTRY_SIZE  = 0x2C,
	UTF_FILE        = 0x80,
	UTF_FOLDER      = 0x10;

const UTF_ENTRY_READER = Symbol("UTF entry origin reader");

/**
 * UTF entry
 * 
 * @property {Number}            offset      Offset position relative to tree
 * @property {Number}            nameOffset  Offset to entry name in dictionary
 * @property {Number}            nextOffset  Offset to next sibling relative to tree
 * @property {Number}            childOffset Offset to first child or data if dataSize > 0
 * @property {Number}            dataSize    Size of data (0 if folder)
 * @property {String}            tag         Entry name in lower-case
 * @property {ArrayBufferWalker} data        Entry data (unless entry is a folder)
 * @property {Boolean}           hasSibling  Has next sibling entry
 * @property {Boolean}           hasChild    Has child entry (entry is a folder)
 * @property {Boolean}           hasData     Entry is file and isn't empty
 */
export class UTFEntry {
	constructor(offset, nameOffset = 0, nextOffset = 0, childOffset = 0, dataSize = 0) {
		this.offset      = offset;
		this.nameOffset  = nameOffset;
		this.nextOffset  = nextOffset;
		this.childOffset = childOffset;
		this.dataSize    = dataSize;
	}

	get reader()     { return this[UTF_ENTRY_READER]; }
	get name()       { return this[UTF_ENTRY_READER].getEntryName(this.nameOffset); }
	get tag()        { return this.name.toLowerCase(); }
	get hasSibling() { return this.nextOffset > 0; }
	get hasChild()   { return this.dataSize == 0 && this.childOffset > 0; }
	get hasData()    { return this.dataSize > 0 && this.childOffset >= 0; }
	get data() {
		if (! (this.reader instanceof UTFReader && this.hasData)) return false;
		if (this.childOffset + this.dataSize > this.reader.data.size) throw new RangeError("Entry data is out of bounds");

		return new ArrayBufferWalker(this.reader.data, this.childOffset, this.dataSize);
	}

	/**
	 * Find entries by tags
	 * @param  {...String} tag Entry tags to search for
	 * @return {...UTFEntry}
	 */
	find(...tags) {
		if (! this.hasChild) return [];

		tags = tags.map(tag => typeof tag == "string" ? tag.toLowerCase() : undefined); // Force lowercase
		let entries = new Array(tags.length),
			counter = tags.reduce((count, tag) => typeof tag == "string" ? count + 1 : count, 0),
			index;

		for (let [tag, child] of this) {
			if (! counter) break; // All were found
			else if ((index = tags.indexOf(tag)) >= 0 && entries[index] == undefined) {
				entries[index] = child;
				counter--;
			}
		}

		return entries;
	}

	/**
	 * Loop over entry children
	 * @yield {[String, UTFEntry]}
	 */
	*[Symbol.iterator]() {
		if (! this.hasChild) return false;

		let offset = this.childOffset, entry;

		while (offset > 0) {
			entry  = this.reader.getEntry(offset);
			offset = entry.nextOffset;
			yield [entry.tag, entry];
		}
	}
}

/**
 * UTF reader
 * 
 * @property {ArrayBuffer} tree     Hierarchy tree
 * @property {Uint8Array}  names    Entry names dictionary
 * @property {ArrayBuffer} data     Entry data chunks
 * @property {String}      filename Origin filename
 */
export class UTFReader {
	constructor() {
		this.tree     = undefined;
		this.names    = undefined;
		this.data     = undefined;
		this.filename = undefined;
	}

	/**
	 * Load UTF asynchronously from Blob object
	 * 
	 * @param  {File|Blob} utf Blob or File object
	 * @return {AsyncFunction}
	 */
	static async load(utf) {
		if (! (utf instanceof Blob)) throw new TypeError("Invalid blob object");
		if (utf.size < UTF_HEADER_SIZE + UTF_ENTRY_SIZE) throw new RangeError("Blob size too small for UTF");

		let [signature, version, treeOffset, treeSize, unusedOffset, entrySize, namesOffset, namesSize, namesUsed, dataOffset] = new Uint32Array(await readBlob(utf, 0, UTF_HEADER_SIZE));
		
		/** @type {UTFReader} Reader instance (without calling constructor) */
		const reader = Object.create(this.prototype);

		// Validate header
		if (signature != UTF_SIGNATURE)  throw new TypeError("Invalid UTF file");
		if (version != UTF_VERSION)      throw new TypeError("Invalid UTF version");
		if (entrySize != UTF_ENTRY_SIZE) throw new TypeError("Invalid UTF entry size");
		if (treeOffset > utf.size)       throw new RangeError("Tree offset is out of bounds");
		if (treeSize == 0)               throw new RangeError("Tree has no size");
		if (namesOffset > utf.size)      throw new RangeError("Dictionary offset is out of bounds");
		if (namesUsed > namesSize)       throw new RangeError("Dictionary used size exceeds allocated size");
		if (dataOffset > utf.size)       throw new RangeError("Data offset is out of bounds");

		// Usually we load external file so there might be filename too
		if (utf instanceof File) reader.filename = utf.name;

		// Read each part
		let [tree, names, data] = await Promise.all([
			readBlob(utf, treeOffset, treeSize),
			readBlob(utf, namesOffset, namesSize),
			readBlob(utf, dataOffset)
		]);

		// Assign buffers
		reader.tree  = tree;
		reader.names = new Uint8Array(names);
		reader.data  = data;

		return reader;
	}

	/**
	 * Get entry at offset
	 * @param  {Number} offset Entry offset in UTF tree
	 * @return {[name, UTFEntry]}
	 */
	getEntry(offset = 0) {
		if (! (this.tree instanceof ArrayBuffer)) throw new TypeError("Reader has no tree buffer");
		if (! Number.isInteger(offset) || offset < 0 || offset > this.tree.size - 8) throw new RangeError("Invalid entry offset value");

		// Get entry properties
		let [nextOffset, nameOffset, fileAttributes, sharingAttributes, childOffset, dataSizeAllocated, dataSizeUsed, dataSizeUncompressed] = new Uint32Array(this.tree, offset, UTF_ENTRY_SIZE / Uint32Array.BYTES_PER_ELEMENT);

		// Some Freelancer UTF files erroneously have folders entries (0x10) with dataSizeUsed > 0
		if (fileAttributes & UTF_FOLDER) dataSizeUsed = 0;

		// Create entry and assign reader
		let entry = new UTFEntry(offset, nameOffset, nextOffset, childOffset, dataSizeUsed);
		entry[UTF_ENTRY_READER] = this;
		
		return entry;
	}

	/**
	 * Get entry name
	 * @param  {Number} nameOffset
	 * @return {String}
	 */
	getEntryName(nameOffset = 0) {
		return String.fromCharCode(...this.names.subarray(nameOffset, this.names.indexOf(0, nameOffset)));
	}

	/**
	 * Return root entry
	 * @return {UTFEntry} Root entry
	 */
	get root() {
		return this.getEntry(0);
	}
}