/**
 * In Freelancer mesh format structure was designed for Direct3D implementation,
 * building from index buffer and interleaved vertex buffer whose contents are
 * specified by FVF (Flexible Vertex Format, outdated in modern Direct3D).
 *
 * Buffers are stored in VMeshData, which also stores mesh groups each
 * specifying index/vertex range in structure buffers as well as a direct
 * reference (via FLCRC32 hash) to material used.
 *
 * Freelancer uses IDirect3DDevice9::DrawIndexedPrimitive with
 * D3DPT_TRIANGLELIST to draw meshes. MinVertexIndex is from VMeshRef.
 *
 * Using same data in WebGL comes with a few quirks. Lack of
 * ARB_draw_elements_base_vertex extension prevents from using index buffer
 * as-is without creating buffers per mesh group.
 */
import {ArrayBufferWalker} from "./core.js";
import {UTFEntry} from "./utf.js";

// Draw modes
export const modes = Object.create(null);

modes.POINTS         = 0; // gl.POINTS
modes.LINE_STRIP     = 1; // gl.LINE_STRIP
modes.LINE_LOOP      = 2; // gl.LINE_LOOP
modes.LINES          = 3; // gl.LINES
modes.TRIANGLE_STRIP = 4; // gl.TRIANGLE_STRIP
modes.TRIANGLE_FAN   = 5; // gl.TRIANGLE_FAN
modes.TRIANGLES      = 6; // gl.TRIANGLES

const FVF_POSITION    = 0x2,   // Position vector (D3DFVF_XYZ)
	FVF_NORMAL        = 0x10,  // Normal vector (D3DFVF_NORMAL)
	FVF_DIFFUSE       = 0x40,  // Diffuse color (D3DFVF_DIFFUSE)
	FVF_SPECULAR      = 0x80,  // Specular color (D3DFVF_SPECULAR)
	FVF_MAP_MASK      = 0xF00, // Texture map count mask (D3DFVF_TEXCOUNT_MASK)
	FVF_POSITION_SIZE = Float32Array.BYTES_PER_ELEMENT * 3,
	FVF_NORMAL_SIZE   = Float32Array.BYTES_PER_ELEMENT * 3,
	FVF_DIFFUSE_SIZE  = Uint8Array.BYTES_PER_ELEMENT * 4,
	FVF_SPECULAR_SIZE = Uint8Array.BYTES_PER_ELEMENT * 4,
	FVF_MAP_SIZE      = Float32Array.BYTES_PER_ELEMENT * 2;

const VMESHREF_SIZE = 0x3C,
	VMESHGROUP_PAD = 0xCC;

const TAG_MESH_DATA = "vmeshdata";

/**
 * VMeshRef bounding box
 *
 * @property {Float32Array} minimum Minimum position
 * @property {Float32Array} maximum Maximum position
 * @property {Float32Array} size    Bounding box dimensions
 */
class BoundingBox {
	constructor(maxX = 0, minX = 0, maxY = 0, minY = 0, maxZ = 0, minZ = 0) {
		this.minimum = new Float32Array([minX, minY, minZ]);
		this.maximum = new Float32Array([maxX, maxY, maxZ]);
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object");

		let [maxX, minX, maxY, minY, maxZ, minZ] = data.readFloat32(6);

		this.minimum.set([minX, minY, minZ]);
		this.maximum.set([maxX, maxY, maxZ]);

		return true;
	}

	get size() {
		return [this.maximum[0] - this.minimum[0], this.maximum[1] - this.minimum[1], this.maximum[2] - this.minimum[2]];
	}
}

/**
 * VMeshRef bounding sphere
 * 
 * Circumscribed sphere containing all vertices of groups belonging to the part
 * and used for geometry frame clipping.
 * 
 * @property {Float32Array} center    Sphere center relative to constraint/origin
 * @property {Number}       radius    Sphere radius
 * @property {Number}       maxRadius Sphere radius + center length
 */
class BoundingSphere {
	constructor(x = 0, y = 0, z = 0, radius = 0) {
		this.center = new Float32Array([x, y, z]);
		this.radius = radius;
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object");

		let [x, y, z, radius] = data.readFloat32(4);

		this.center.set([x, y, z]);
		this.radius = radius;

		return true;
	}

	/**
	 * Get max bounding radius
	 * @return {Number}
	 */
	get maxRadius() {
		return Math.sqrt(this.center[0] * this.center[0] + this.center[1] * this.center[1] + this.center[2] * this.center[2]) + this.radius;
	}
}

/**
 * Reference points to chunk in VMeshData used to draw LOD mesh
 *
 * @property {Number} meshID      Mesh identifier
 * @property {Number} vertexStart
 * @property {Number} vertexCount
 * @property {Number} indexStart
 * @property {Number} indexCount
 * @property {Number} groupStart
 * @property {Number} groupCount
 */
export class VMeshRef {
	constructor(meshID = 0, vertexStart = 0, vertexCount = 0, indexStart = 0, indexCount = 0, groupStart = 0, groupCount = 0) {
		this.meshID      = meshID;
		this.vertexStart = vertexStart;
		this.vertexCount = vertexCount;
		this.indexStart  = indexStart;
		this.indexCount  = indexCount;
		this.groupStart  = groupStart;
		this.groupCount  = groupCount;
		this.box         = new BoundingBox();
		this.sphere      = new BoundingSphere();
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object");

		let size;

		[size, this.meshID] = data.readInt32(2);

		if (size != VMESHREF_SIZE) throw new RangeError("Invalid mesh reference size");

		[this.vertexStart, this.vertexCount, this.indexStart, this.indexCount, this.groupStart, this.groupCount] = data.readInt16(6);

		this.box.load(data);
		this.sphere.load(data);

		return true;
	}
}

/**
 * Material group in VMeshData
 *
 * In vanilla files unknown property might be a draw mode.
 *
 * @property {Number} materialID  Material identifier
 * @property {Number} vertexStart
 * @property {Number} vertexEnd
 * @property {Number} vertexCount
 * @property {Number} indexCount
 * @property {Number} unknown     Some tools consider this padding
 * @property {Number} drawMode    Render draw mode (points, lines, triangles)
 */
export class VMeshGroup {
	constructor(materialID = 0, vertexStart = 0, vertexEnd = 0, indexCount = 0, drawMode = modes.TRIANGLES) {
		this.materialID  = materialID;
		this.vertexStart = vertexStart;
		this.vertexEnd   = vertexEnd;
		this.indexCount  = indexCount;
		this.unknown     = 0;
		this.drawMode    = drawMode;
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object");

		[this.materialID] = data.readInt32(1);
		[this.vertexStart, this.vertexEnd, this.indexCount, this.unknown] = data.readInt16(4);

		return true;
	}

	/**
	 * Get vertex count in mesh group
	 * @return {Number}
	 */
	get vertexCount() {
		return this.vertexEnd - this.vertexStart + 1;
	}
}

/**
 * Mesh data contains index and vertex buffers
 * Format determines vertex buffer attributes (D3D Flexible Vertex Format)
 * Portions are pulled via VMeshRef
 *
 * @property {Number}      type           Mesh format?
 * @property {Number}      surface        Hitbox format?
 * @property {Number}      format         Vertex data format (FVF)
 * @property {Number}      scale          Default scale to render
 * @property {Array}       groups         Material groups
 * @property {Uint16Array} indices        Triangle indices
 * @property {Uint8Array}  vertices       Vertex data buffer
 * @property {Boolean}     hasPosition
 * @property {Boolean}     hasNormal
 * @property {Boolean}     hasDiffuse
 * @property {Boolean}     hasSpecular
 * @property {Number}      mapCount
 * @property {Number}      vertexLength
 * @property {Number}      vertexCount
 * @property {Number}      positionOffset
 * @property {Number}      normalOffset
 * @property {Number}      diffuseOffset
 * @property {Number}      specularOffset
 * @property {Number}      mapOffset
 */
export class VMeshData {
	constructor(type = 1, surface = 4, format = 0) {
		this.type     = type;
		this.surface  = surface;
		this.format   = format;
		this.scale    = 1;
		this.groups   = [];
		this.indices  = undefined;
		this.vertices = undefined;
	}

	/**
	 * Set FVF bitmask to object properties
	 * @param {Number} mask
	 */
	set format(mask) {
		this.hasPosition  = (mask & FVF_POSITION) > 0;
		this.hasNormal    = (mask & FVF_NORMAL) > 0;
		this.hasDiffuse   = (mask & FVF_DIFFUSE) > 0;
		this.hasSpecular  = (mask & FVF_SPECULAR) > 0;
		this.mapCount     = (mask & FVF_MAP_MASK) >> 8;
		this.vertexLength = 0;

		if (this.hasPosition) this.vertexLength += FVF_POSITION_SIZE;
		if (this.hasNormal)   this.vertexLength += FVF_NORMAL_SIZE;
		if (this.hasDiffuse)  this.vertexLength += FVF_DIFFUSE_SIZE;
		if (this.hasSpecular) this.vertexLength += FVF_SPECULAR_SIZE;

		this.vertexLength += this.mapCount * FVF_MAP_SIZE;

		this.positionOffset = this.hasPosition ? 0 : undefined;
		this.normalOffset   = this.hasNormal   ? (this.hasPosition ? FVF_POSITION_SIZE : 0) : undefined;
		this.diffuseOffset  = this.hasDiffuse  ? (this.hasPosition ? FVF_POSITION_SIZE : 0) + (this.hasNormal ? FVF_NORMAL_SIZE : 0) : undefined;
		this.specularOffset = this.hasSpecular ? (this.hasPosition ? FVF_POSITION_SIZE : 0) + (this.hasNormal ? FVF_NORMAL_SIZE : 0) + (this.hasDiffuse ? FVF_DIFFUSE_SIZE : 0) : undefined;
		this.mapOffset      = this.mapCount    ? (this.hasPosition ? FVF_POSITION_SIZE : 0) + (this.hasNormal ? FVF_NORMAL_SIZE : 0) + (this.hasDiffuse ? FVF_DIFFUSE_SIZE : 0) + (this.hasSpecular ? FVF_SPECULAR_SIZE : 0) : undefined;
	}

	/**
	 * Get FVF bitmask from object properties
	 * @return {Number}
	 */
	get format() {
		let mask = 0;

		if (this.hasPosition) mask |= FVF_POSITION;
		if (this.hasNormal)   mask |= FVF_NORMAL;
		if (this.hasDiffuse)  mask |= FVF_DIFFUSE;
		if (this.hasSpecular) mask |= FVF_SPECULAR;
		mask |= (this.mapCount << 8);

		return mask;
	}

	/**
	 * Get vertex count in buffer
	 * @return {Number}
	 */
	get vertexCount() {
		return this.vertices.byteLength / this.vertexLength;
	}

	/**
	 * @param  {Number} index
	 * @return {Number}
	 */
	getMapOffset(index = 0) {
		return index < this.mapCount ? this.mapOffset + index * FVF_MAP_SIZE : null;
	}

	*getGroupsByReference(reference) {
		for (let i = 0; i < reference.groupCount; i++) yield this.groups[reference.groupStart + i];
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		if (! (data instanceof ArrayBufferWalker)) throw new TypeError("Invalid data object");

		let [type, surface] = data.readInt32(2);

		if (type != 0x1) throw new TypeError("Unrecognized mesh type");
		if (surface != 0x4) throw new TypeError("Unrecognized surface type");

		let [groupCount, indexCount, format, vertexCount] = data.readInt16(4);

		this.format = format; // D3D FVF mask flags
		this.groups = [];

		// Read mesh groups	
		for (let g = 0; g < groupCount; g++) (this.groups[g] = new VMeshGroup()).load(data); 

		// Read indices
		this.indices  = data.readInt16(indexCount);

		// Read vertices
		this.vertices = data.readInt8(vertexCount * this.vertexLength);

		if (vertexCount != this.vertexCount) throw new RangeError("Mesh has invalid number of vertices");
		return true;	
	}

	/**
	 * Returns array of position vectors for wireframe buffer data
	 * @return {Float32Array}
	 */
	getPositions() {
		if (! this.hasPosition) return false;

		const vertexCount = this.vertexCount,
			vertexLength = this.vertexLength,
			vertexView = new DataView(this.vertices.buffer, this.vertices.byteOffset, this.vertices.byteLength),
			positions = new Float32Array(vertexCount * 3);

		for (let v = 0, p = 0, e = 0; v < vertexCount; v++) {
			e = v * vertexLength;

			positions[p++] = vertexView.getFloat32(e, true);
			positions[p++] = vertexView.getFloat32(e + 4, true);
			positions[p++] = vertexView.getFloat32(e + 8, true);
		}

		return positions;
	}
}

/**
 * Wireframes displayed in HUD (GL.LINES)
 *
 * @property {Number}      meshID      Mesh identifier
 * @property {Number}      vertexStart
 * @property {Number}      vertexCount
 * @property {Number}      vertexRange
 * @property {Uint16Array} indices     Line indices
 */
export class VMeshWire {
	constructor(meshID = 0, vertexStart = 0, vertexCount = 0, vertexRange = 0) {
		this.meshID      = meshID;
		this.vertexStart = vertexStart;
		this.vertexCount = vertexCount;
		this.vertexRange = vertexRange;
		this.indices     = undefined;
	}

	/**
	 * Load data
	 * @param  {ArrayBufferWalker} data
	 * @return {Boolean}
	 */
	load(data) {
		let size;

		[size, this.meshID] = data.readInt32(2);

		if (size != 16) throw new RangeError("Invalid wire reference size");
		
		[this.vertexStart, this.vertexCount, this.indexCount, this.vertexRange] = data.readInt16(4);

		this.indices = data.readInt16(this.indexCount);
		return true;
	}
}

/**
 * Load mesh from UTF
 * @param  {UTFEntry}  folder
 * @return {VMeshData}
 */
export function loadMesh(folder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid texture UTF entry object");

	let [entry] = folder.find(TAG_MESH_DATA);
	if (! entry) return false;

	let mesh = new VMeshData();
	mesh.load(entry.data);

	return mesh;
}