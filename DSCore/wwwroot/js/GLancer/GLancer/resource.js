/**
 * Resource management implements synchronous and asynchronous resources.
 *
 * Textures, materials and meshes are synchronous types of resources. They are
 * referenced directly by hash without indication of external UTF containing
 * them.
 *
 * Models on the other hand are asynchronous resources. They are referenced by
 * filename and are loaded on demand.
 *
 * Glancer adds two new types: shaders and shapes. In Freelancer shaders were
 * hardcoded directly in flmaterials.dll and shading.dll. Shapes are a
 * simplified VMeshData resource for drawing wireframe objects.
 */
import {Glancer} from "./system.js";
import {UTFReader, UTFEntry} from "./utf.js";
import {Texture, loadTexture} from "./texture.js";
import {Material, loadMaterial} from "./material.js";
import {Shader, loadShader} from "./shader.js";
import {VMeshData, VMeshRef, loadMesh} from "./vmesh.js";
import {ShapeMeshData, loadShape} from "./shape.js";
import {SimpleModel} from "./compound.js";
import {loadRigidModel} from "./rigid.js";
import {flcrc32} from "./hash.js";

export const typeMask = Object.create(null);

typeMask.TEXTURES   = 1;
typeMask.MATERIALS  = 2;
typeMask.SHADERS    = 4;
typeMask.MESHES     = 8;
typeMask.SHAPES     = 16;

const TAG_TEXTURES = "texture library",
	TAG_MATERIALS  = "material library",
	TAG_MESHES     = "vmeshlibrary",
	TAG_SHADERS    = "shader library",
	TAG_SHAPES     = "shape library";

const TEXTURE_PLACEHOLDER = new Texture(),
	MATERIAL_PLACEHOLDER  = new Material(),
	MESH_PLACEHOLDER      = null; // ?box?

MATERIAL_PLACEHOLDER.diffuseTexture = "null";

/**
 * List resources from UTF folder which aren't duplicated in resource library
 * @param {UTFEntry} folder        UTF entry representing resource
 * @param {function} loader        Resource constructor function
 * @param {Map}      library       Resource map to look for duplicates
 * @param {Object}   placeholder   Placeholder resource to match against
 * @yield {[Number, Object]}
 */
function addResources(folder, loader, library, placeholder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object type");
	if (! folder.hasChild) return false;
	if (typeof loader != "function") return false;

	let count = 0;
	
	for (let [tag, entry] of folder) {
		if (! entry.hasChild) continue;

		let hash = flcrc32(tag);

		// Skip already loaded resources unless placeholder is specified and
		// resource in library is placeholder resource.
		if (library && library.has(hash) && (! placeholder || library.get(hash) != placeholder)) continue; 

		let resource = loader(entry);
		if (! resource) continue;

		library.set(hash, resource);
		count++;
	}

	return true;
}

/**
 *
 * Textures, materials and meshes are referenced by hash, we either have them
 * or not, and in case of texture and material we can return a placeholder.
 *
 * Models are referenced by relative file path but can be loaded on demand,
 * getting these is always asynchronous task.
 * 
 * @param {FileLoader} loader    Loader interface to handle external files
 * @param {Map}        textures  Shared textures
 * @param {Map}        materials Shared materials
 * @param {Map}        meshes    Shared rigid meshes (VMeshData)
 * @param {Map}        models    Shared models (Rigid/Deformable)
 * @param {Map}        shaders   Shared shaders (only source, not compiled or linked)
 */
export class Resources {
	constructor() {
		this.textures  = new Map(); // Hash->Texture
		this.materials = new Map(); // Hash->Material
		this.meshes    = new Map(); // Hash->VMeshData
		this.shapes    = new Map(); // Hash->ShapeMeshData
		this.models    = new Map(); // Hash->SimpleModel/CompoundModel/etc
		this.shaders   = new Map(); // Hash->Shader
	}

	/**
	 * Get Texture object from resource library
	 * @param  {String|Number} name
	 * @return {Texture}
	 */
	getTexture(name) {
		let hash = flcrc32(name), texture = this.textures.get(hash);

		if (! texture) this.textures.set(hash, texture = TEXTURE_PLACEHOLDER);
		return texture;
	}

	/**
	 * Get Material object from resource library
	 * @param  {String|Number} name
	 * @return {Material}
	 */
	getMaterial(name) {
		let hash = flcrc32(name), material = this.materials.get(hash);

		if (! material) this.materials.set(hash, material = MATERIAL_PLACEHOLDER);
		return material;
	}

	/**
	 * Get VMeshData object from resource library
	 * @param  {String|Number} name
	 * @return {VMeshData}
	 */
	getMesh(name) {
		return this.meshes.get(flcrc32(name));
	}

	/**
	 * Get VMeshData object from resource library
	 * @param  {String|Number} name
	 * @return {VMeshData}
	 */
	getShape(name) {
		return this.shapes.get(flcrc32(name));
	}

	/**
	 * Get Shader object from resource library
	 * @param  {String|Number} name
	 * @return {Shader}
	 */
	getShader(name) {
		return this.shaders.get(flcrc32(name));
	}

	/**
	 * Get model from resources (request if absent)
	 * @param  {String} filename Path to file (ex. "SHIPS/LIBERTY/li_elite/li_elite.cmp")
	 * @param  {Number} type     Model type (Sphere, Rigid, Deformable)
	 * @return {AsyncFunction}   Deferred promise, resolves model object
	 */
	async getModel(filename, size = 1) {
		let hash = flcrc32(filename), model = this.models.get(hash);

		if (model instanceof SimpleModel) return model;
		if (model instanceof Promise) return model; // Already loading

		// Request
		if (typeof model == "undefined") {
			// The trick here is to set Promise to model library to prevent
			// duplicate loading requests.

			let promise = Glancer.loader.loadUTF(filename, size).then(reader => {
				// Model files contain meshes, sometimes textures and materials
				this.loadLibraries(reader, typeMask.TEXTURES | typeMask.MATERIALS | typeMask.MESHES);

				// TODO: Switch between types
				return loadRigidModel(reader);
			});

			this.models.set(hash, promise);

			// TODO: handle error cases:
			// A) file does not exist
			// B) ...but has errors while reading UTF
			// C) ...but has no Sphere/Cmpnd/VMeshRef/etc for a model
			// D) ...but has errors while loading a model
			this.models.set(hash, model = await promise);
			return model;
		}

		throw new RangeError("Referenced model does not exist");
	}

	/**
	 * Get shared resources from UTF
	 * @param  {String} filename
	 * @param  {Number} size
	 * @return {UTFReader}
	 */
	async getResources(filename, size = 1, types) {
		let reader = await Glancer.loader.loadUTF(filename, size);

		this.loadLibraries(reader, types);
		return reader;
	}

	/**
	 * Load libraries into resource maps
	 * 
	 * @param {UTFReader} reader 
	 * @param {Number}    types  Bitmask
	 */
	loadLibraries(reader, types = typeMask.TEXTURES | typeMask.MATERIALS) {
		if (! (reader instanceof UTFReader)) throw new TypeError("Invalid UTF reader object type");

		let libraries = [], count = 0;

		// Setup list of library tags to search for
		if (types & typeMask.TEXTURES)  libraries[0] = TAG_TEXTURES;
		if (types & typeMask.MATERIALS) libraries[1] = TAG_MATERIALS;
		if (types & typeMask.SHADERS)   libraries[2] = TAG_SHADERS;
		if (types & typeMask.MESHES)    libraries[3] = TAG_MESHES;
		if (types & typeMask.SHAPES)    libraries[4] = TAG_SHAPES;

		// Find libraries
		let [textures, materials, shaders, meshes, shapes] = reader.root.find(...libraries);

		// Process found libraries
		if (textures)  count += addResources(textures,  loadTexture,  this.textures,  TEXTURE_PLACEHOLDER);
		if (materials) count += addResources(materials, loadMaterial, this.materials, MATERIAL_PLACEHOLDER);
		if (shaders)   count += addResources(shaders,   loadShader,   this.shaders);
		if (meshes)    count += addResources(meshes,    loadMesh,     this.meshes);
		if (shapes)    count += addResources(shapes,    loadShape,    this.shapes);

		return count;
	}

	async loadShadersXML(filename, size) {
		const xml = await Glancer.loader.loadXML(filename, size);
		if (! (xml instanceof XMLDocument)) throw new TypeError("Invalid XML document object");

		for (let element of xml.documentElement.children) {
			if (! element.hasChildNodes()) continue;

			switch (element.nodeName) {
				case "program":
					let name = element.getAttribute("name"),
						shader = new Shader();

					shader.loadXML(element);
					this.shaders.set(flcrc32(name), shader);
				break;
			}
		}

		return true;
	}

	/**
	 * Get part of VMeshData by reference
	 * New object will have only groups, indices and vertices referenced/copied.
	 * 
	 * @param {VMeshRef} reference
	 * @return {VMeshData}
	 */
	getMeshByReference(reference, copy = false) {
		if (! (reference instanceof VMeshRef)) return false;

		/** @type {VMeshData} Referenced mesh data */
		let data = this.getMesh(reference.meshID);
		if (! (data instanceof VMeshData)) return false;

		// TODO: validate reference here for group range out of bounds, index range out of bounds, vertex range out of bounds.

		let result = new VMeshData(data.type, data.surface, data.format);

		result.groups   = data.groups.slice(reference.groupStart, reference.groupStart + reference.groupCount);
		result.indices  = data.indices.subarray(reference.indexStart, reference.indexStart + reference.indexCount);
		result.vertices = data.vertices.subarray(reference.vertexStart * data.vertexLength, (reference.vertexStart + reference.vertexCount) * data.vertexLength);

		if (copy) {
			result.indices  = result.indices.slice();
			result.vertices = result.vertices.slice();
		}

		return result;
	}
}

