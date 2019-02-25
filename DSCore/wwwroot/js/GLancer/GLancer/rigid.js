import {getRangeIndex} from "./core.js";
import {UTFReader, UTFEntry} from "./utf.js";
import {VMeshWire, VMeshRef} from "./vmesh.js";
import {CompoundModel, SimpleModel} from "./compound.js";
import {loadHardpoints} from "./hardpoint.js";
import {vec3, mat4} from "../GLmatrix/index.js";

const LEVEL_PATTERN = /^Level(\d+)$/i;

const TAG_COMPOUND     = "cmpnd",
	TAG_MULTILEVEL     = "multilevel",
	TAG_LOD_SWITCH     = "switch2",
	TAG_MESH_PART      = "vmeshpart",
	TAG_MESH_REFERENCE = "vmeshref",
	TAG_WIRE_PART      = "vmeshwire",
	TAG_WIRE_REFERENCE = "vwiredata";

/**
 * Rigid meshes LODs
 * 
 * @property {Array} references
 * @property {Array} ranges
 */
export class MultiLevel {
	constructor() {
		this.ranges     = [0];
		this.references = [];
	}

	/**
	 * @param {Number}   level
	 * @param {VMeshRef} reference
	 * @param {Number}   range
	 */
	setLevel(level, reference, range) {
		if (! (reference instanceof VMeshRef)) throw new TypeError("Invalid mesh reference object type");

		this.setRange(level, range);
		this.references[level] = reference;
		this.ranges[level + 1] = range;
		return true;
	}

	/**
	 * @param {Number}
	 * @param {Number}
	 */
	setRange(level, range) {
		if (typeof range != "number" || range <= 0) throw new RangeError("Invalid level range");

	}

	/**
	 * Get LOD mesh reference by distance
	 * @param  {Number} distance
	 * @return {VMeshRef}
	 */
	getLODReference(distance) {
		let level = getRangeIndex(distance, this.ranges);
		return this.references[level];
	}

	/**
	 * Load from UTF
	 * @param  {UTFEntry} folder `MultiLevel` entry
	 * @return {Number}
	 */
	load(folder) {
		let match, part, reference, level;

		this.references = []; // Reset levels

		for (let [tag, entry] of folder) switch (tag) {
			case TAG_LOD_SWITCH: this.ranges = Array.from(entry.data.readFloat32()); continue;
			default:

			if (! (match = LEVEL_PATTERN.exec(tag))) continue;

			level = parseInt(match[1]);
			
			if (([part] = entry.find(TAG_MESH_PART)) && ([reference] = part.find(TAG_MESH_REFERENCE)))
				(this.references[level] = new VMeshRef()).load(reference.data);
		}

		// Ensure there are no gaps in levels
		if ((level = this.references.indexOf(undefined)) >= 0) throw new RangeError("Missing reference data for LOD " + level);
		return this.references.length;
	}
}

/**
 * Rigid vmesh part
 * 
 * @property {VMeshRef|MultiLevel} mesh
 * @property {VMeshWire}           wireframe HUD wireframe
 */
export class RigidPart {
	constructor() {
		this.mesh      = undefined;
		this.wireframe = undefined;
	}

	/**
	 * Load hardpoints, wireframe, reference/levels from UTF fragment
	 * @param  {UTFEntry} folder
	 * @return {Boolean}
	 */
	load(folder) {
		this.wireframe = undefined;

		let [wireframe, part, lods] = folder.find(TAG_WIRE_PART, TAG_MESH_PART, TAG_MULTILEVEL),
			meshReference, wireReference;

		// Part can have either MultiLevel for one or more LODs, or single VMeshPart
		if (lods) (this.mesh = new MultiLevel()).load(lods);
		else if (part && ([meshReference] = part.find(TAG_MESH_REFERENCE))) (this.mesh = new VMeshRef()).load(meshReference.data);
		else throw new RangeError("Part is missing mesh reference");

		// Check for HUD wireframe		
		if (wireframe && ([wireReference] = wireframe.find(TAG_WIRE_REFERENCE))) (this.wireframe = new VMeshWire()).load(wireReference.data);
		return true;
	}

	/**
	 * Get LOD mesh reference by distance
	 * @param  {Number} distance
	 * @return {VMeshRef}
	 */
	getLODReference(distance) {
		if (this.mesh instanceof MultiLevel) return this.mesh.getLODReference(distance);
		if (this.mesh instanceof VMeshRef)   return this.mesh;
		return false;
	}

	/**
	 * Get specific LOD level
	 * @param  {Number} level
	 * @return {VMeshRef}
	 */
	getLOD(level) {
		if (this.mesh instanceof MultiLevel && level < this.mesh.references.length) return this.mesh.references[level];
		else if (this.mesh instanceof VMeshRef && level < 1) return this.mesh;
		return false;
	}
}

/**
 * Modification of compound model to facilitate rendering needs
 */
export class CompoundRigidModel extends CompoundModel {

	/**
	 * Load compound hierarchy and part fragments
	 * 
	 * @param  {UTFEntry} compound  `Cmpnd` entry
	 * @param  {UTFEntry} fragments `*.3db` entry
	 * @return {Boolean}
	 */
	load(compound, fragments) {
		return super.load(compound, fragments, RigidPart);
	}

	/**
	 * Get encompassing radius for idle state
	 * @return {Number}
	 */
	getRadius() {
		if (! this.root) return undefined;

		let radius   = 0,
			model    = mat4.create(),
			position = vec3.create();

		for (let [part, reference] of this.getMeshes(0)) {
			mat4.fromTranslation(model, reference.sphere.center);
			this.getPartTransform(model, part);
			mat4.getTranslation(position, model);

			let r = vec3.length(position) + vec3.length(reference.sphere.center) + reference.sphere.radius;

			if (r > radius) radius = r;
		}

		return radius;
	}

	/**
	 * Get every part mesh and mesh references
	 * 
	 * @yield {[RigidPart, VMeshData, VMeshRef]} {part, mesh, reference}
	 */
	*getMeshes(distance = 0) {
		// let meshID, mesh, reference;

		for (let part of this.parts.values()) {
			let reference = part.getLODReference(distance);
			if (! reference) continue;

			/*
			if (meshID != reference.meshID) mesh = Glancer.resources.getMesh(meshID = reference.meshID);
			if (! mesh) continue;
			*/

			yield [part, reference];
		}
	}

	/**
	 * Get every part mesh and wireframe reference
	 * @yield {[RigidPart, VMeshData, VWireData]} {part, mesh, reference}
	 */
	*getWireframes(model) {
		let meshID, mesh;

		for (let part of this.parts.values()) {
			if (! part.wireframe || ! part.wireframe.meshID) continue;

			if (meshID != part.wireframe.meshID) mesh = Glancer.resources.getMesh(meshID = part.wireframe.meshID);
			if (! mesh) continue;

			yield [part, mesh, part.wireframe];
		}
	}
}

/**
 * Same
 */
export class SimpleRigidModel extends SimpleModel {

	/**
	 * Load root part
	 * 
	 * @param  {UTFEntry} fragment `\` entry
	 * @return {Boolean}
	 */
	load(fragment) {
		this.root = new RigidPart();
		this.root.load(fragment);

		for (let [name, hardpoint] of loadHardpoints(fragment))
			this.attachHardpoint(name, hardpoint);

		return true;
	}

	/**
	 * Get mesh radius from root part
	 * @return {Number}
	 */
	getRadius() {
		if (! this.root) return undefined;
		let reference = this.root.getLODReference(0);
		return reference.sphere.radius;
	}

	/**
	 * Get root part by distance
	 * 
	 * @param  {Number} distance
	 * @return {VMeshRef}
	 */
	*getMeshes(distance = 0) {
		if (! this.root) return false;

		let reference = this.root.getLODReference(distance);
		if (! reference) return false;

		let mesh = Glancer.resources.getMesh(meshID = reference.meshID)
		if (! mesh) return false;

		yield [this.root, mesh, reference];
	}
}

export class SphereModel extends SimpleModel {

	load(sphere) {
		let [radius, sides] = sphere.find("radius", "sides");

		[sides]  = sides.data.readInt32(1);
		[radius] = radius.data.readFloat32(1);

		for (let s = 0; s < sides; s++) {
			// M0-6 (6 for sides of spherified cube and 7th is atmosphere)

		}

		// TODO: generate spherified cube mesh LODs given the radius
	}
}

/**
 * Load RigidModel or SimpleModel
 * @param  {UTFReader} reader
 * @return {CompoundRigidModel|SimpleRigidModel}
 */
export function loadRigidModel(reader) {
	if (! (reader instanceof UTFReader)) throw new TypeError("Invalid UTF reader object type");

	let [compound, sphere] = reader.root.find(TAG_COMPOUND, "sphere"), result;

	// Compound model
	if (compound) (result = new CompoundRigidModel()).load(compound, reader.root);
	else if (sphere) (result = new SphereModel()).load(sphere);
	else (result = new SimpleRigidModel()).load(reader.root);

	return result;
}