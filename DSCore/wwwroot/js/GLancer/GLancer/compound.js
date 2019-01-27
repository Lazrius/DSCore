/**
 * Freelancer implements same compound structure both for rigid (.cmp) and
 * deformable models (.3db). Deformable models are always compound, while rigid
 * models without parts can be simple models (.3db).
 *
 * Compound models may have only single part (which has to be root), though
 * would have no advantage over partless simple models.
 *
 * Lastly are also generated sphere models (.sph), they are simple models too
 * and can have hardpoints as well.
 *
 * Originally the structure of objects mimicked intended compound tree (i.e.
 * hardpoints were properties of part objects as opposed to being stored in
 * model object). But has been discarded and rewritten into leaner system for
 * faster access without recursion.
 */

import {UTFEntry} from "./utf.js";
import {loadConstraints, FixedConstraint} from "./constraint.js";
import {loadHardpoints} from "./hardpoint.js";
import {mat4} from "../glmatrix/index.js";

const TAG_ROOT      = "Root",
	TAG_OBJECT_NAME = "object name",
	TAG_INDEX       = "index",
	TAG_FILE_NAME   = "file name";

const transformCache = Symbol("Pre-calculated transformation matrix");

/**
 * At its basic core a simple model contains only one part (root) and hardpoints.
 *
 * @property {Part} root
 * @property {Map}  hardpoints
 */
export class SimpleModel {
	constructor() {
		this.root       = undefined;
		this.hardpoints = new Map();
	}

	/**
	 * Attach hardpoint to model
	 * @param  {String}    name
	 * @param  {Hardpoint} hardpoint
	 * @return {Boolean}
	 */
	attachHardpoint(name, hardpoint) {
		if (typeof name != "string" || ! name.length) throw new TypeError("Invalid hardpoint name");
		if (this.hardpoints.has(name)) throw new RangeError("Hardpoint already exists");

		this.hardpoints.set(name, hardpoint);
		return true;
	}

	/**
	 * Get hardpoint by name
	 * @param  {String} name Typically starts either with "Hp" or "Dp"
	 * @return {Hardpoint}
	 */
	getHardpoint(name) {
		return this.hardpoints.get(name);
	}

	/**
	 * Get hardpoints of root
	 * @return {Map}
	 */
	getHardpoints() {
		return this.hardpoints;
	}
}

/**
 * Compound models consists of one or more parts comprising tree hierarchy with
 * a root part at its base. Parts are attached via constraints specifying
 * physical relation to parent part and setting up limits. Hardpoints are
 * explicitly attached to parts.
 * 
 * @property {Map}     parts       Object name->Part
 * @property {WeakMap} parents     Child->Part (Hardpoint->Part and Part->Part)
 * @property {WeakMap} constraints Child->Constraint (Part->Constraint)
 * @property {Array}   indices     Index->Part
 * @property {Symbol}  transforms  Pre-calculated matrices for fixed parts
 */
export class CompoundModel extends SimpleModel {
	constructor() {
		super();

		this.parts       = new Map();
		this.parents     = new WeakMap();
		this.constraints = new WeakMap();
		this.indices     = [];
	}

	/**
	 * Add compound part to model
	 * @param  {String} name 
	 * @param  {Part}   part
	 * @return {Boolean}
	 */
	addPart(name, part, index = 0) {
		if (typeof name != "string" || ! name.length) throw new TypeError("Invalid compound part name");
		if (this.parts.has(name)) throw new RangeError("Compound part already exists");

		this.parts.set(name, part);
		if (name == TAG_ROOT) this.root = part;

		this.indices[index] = part;
		return true;
	}

	/**
	 * Attach hardpoint to model part
	 * @param  {String}      name      Local hardpoint name
	 * @param  {Hardpoint}   hardpoint
	 * @param  {String|Part} part      
	 * @return {Boolean}
	 */
	attachHardpoint(name, hardpoint, part) {
		if (typeof part == "string" && ! (part = this.parts.get(part))) throw new RangeError("Could not find part with that name");
		// else if (! this.parts.has(part)) throw new RangeError("Compound part does not belong to this model");

		super.attachHardpoint(name, hardpoint);
		this.parents.set(hardpoint, part);
		return true;
	}

	/**
	 * Attach part to part
	 * @param  {String|Part} child
	 * @param  {String|Part} parent
	 * @param  {Constraint}  constraint Attachment constraint
	 * @return {Boolean}
	 */
	attachPart(child, parent, constraint) {
		if (typeof child == "string") child = this.getPart(child);
		//else if (! this.parts.has(child)) throw new RangeError("Missing child compound part");

		if (typeof parent == "string") parent = this.getPart(parent);
		//else if (! this.parts.has(parent)) throw new RangeError("Missing parent compound part");

		this.parents.set(child, parent);
		this.constraints.set(child, constraint);

		return true;
	}

	/**
	 * Get compound part by name
	 * @param  {String} name
	 * @return {Part}
	 */
	getPart(name) {
		return this.parts.get(name);
	}

	/**
	 * Get parent of child
	 * @param  {String|Part|Hardpoint} child
	 * @return {Part}
	 */
	getParent(child) {
		if (typeof child == "string") child = this.getPart(child) || this.getHardpoint(child);

		return this.parents.get(child);
	}

	/**
	 * Get part constraint
	 * @param  {Part} part
	 * @return {Constraint}
	 */
	getConstraint(part) {
		if (typeof part == "string") part = this.getPart(part);
		if (! part) return undefined;

		return this.constraints.get(part);
	}

	/**
	 * Get model matrix of part
	 * 
	 * @param  {Float32Array} out  Model matrix
	 * @param  {Part}         part Compound part
	 * @return {Float32Array}
	 */
	getPartTransform(out, part) {
		if (typeof part == "string") part = this.getPart(part);
		if (! part) return undefined;

		// Apply pre-calculated matrix if available
		if (part[transformCache]) return mat4.multiply(out, out, part[transformCache]);
		
		let constraint,
			parent  = part,
			isFixed = true;

		while (parent && parent != this.root) {
			constraint = this.getConstraint(parent);

			if (! (constraint instanceof FixedConstraint)) isFixed = false;

			// Multiply in reverse order
			mat4.multiply(out, constraint.transform, out);

			// Apply offset if constraint has it
			if (constraint.offset) mat4.translate(out, out, constraint.offset);

			parent = this.getParent(parent);
		}

		if (isFixed) part[transformCache] = mat4.clone(out);
		return out;
	}

	/**
	 * Get all transforms
	 *
	 * @param {Float32Array} out        Model matrix
	 * @param {Boolean}      hardpoints Also hardpoints
	 * @yield {[Part, Float32Array]} [child, matrix]
	 */
	*getTransforms(out, hardpoints = false) {
		if (! (out instanceof Float32Array)) return false;

		for (let [name, part] of this.parts)
			yield [name, part, this.getPartTransform(out, part)];

		/*
		for (let [name, hardpoint] of this.hardpoints)
			yield [name, hardpoint, this.getHardpointTransform]
		*/
	}

	/**
	 * Get part hardpoints
	 * @param {String|Part} parent Parent part
	 * @yield {[String, Part]}
	 */
	*getHardpoints(parent = this.root) {
		for (let [name, hardpoint] of this.hardpoints)
			if (this.parents.get(hardpoint) === parent)
				yield [name, hardpoint];
	}

	/**
	 * Get children compound parts of parent compound part
	 * @param {String|Part} parent Parent part
	 * @yield {[String, Part]}
	 */
	*getChildren(parent = this.root) {
		for (let [name, child] of this.parts)
			if (this.parents.get(child) === parent)
				yield [name, child];
	}

	/**
	 * Load compound model
	 * 
	 * @param  {UTFEntry} compound    `Cmpnd` entry
	 * @param  {UTFEntry} fragments   UTFEntry where part fragments are located
	 * @param  {function} constructor Compound part constructor
	 * @return {Boolean}
	 */
	load(compound, fragments, constructor) {
		if (! compound instanceof UTFEntry) throw new TypeError("Invalid compound entry object type");
		if (! compound.hasChild) throw new RangeError("Compound entry is empty");

		// Clear existing stuff
		this.root = undefined;
		this.parts.clear();
		this.hardpoints.clear();

		// Because WeakMap.prototype.clear doesn't exist
		this.parents     = new WeakMap();
		this.constraints = new WeakMap();

		// By default use root as fragments directory
		if (! fragments) fragments = compound.reader.root;

		// Load parts from `Cmpnd` and UTF fragment
		for (let [name, index, fragment, part] of loadParts(compound, fragments, constructor))
			if (this.addPart(name, part, index)) // Load hardpoints from part UTF fragment and attach to model
				for (let [name, hardpoint] of loadHardpoints(fragment))
					this.attachHardpoint(name, hardpoint, part);

		if (! this.root) throw new RangeError("Compound model is missing Root part");

		// Build hierarchy from constraints (may not exist if compound model consists only of Root part)
		for (let [child, parent, constraint] of loadConstraints(compound))
			this.attachPart(child, parent, constraint);

		return true;
	}
}

/**
 * Load parts from `Cmpnd`
 * 
 * @param {UTFEntry} folder      `Part_*` or `Root` entry
 * @param {UTFEntry} fragments   `\` entry
 * @param {function} constructor Compound part constructor
 * @yield {[String, Number, UTFEntry, Part]}
 */
function* loadParts(folder, fragments, constructor) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid compound UTF entry object type");
	if (! (fragments instanceof UTFEntry)) throw new TypeError("Invalid fragments UTF entry object type");
	if (typeof constructor != "function") throw new TypeError("Invalid compound part constructor type");

	for (let [tag, entry] of folder) {
		if (! (tag == "root" || tag.startsWith("part_")) || ! entry.hasChild) continue;

		// Part name, part index, and embedded fragment entry filename which contains the rest of the part data
		let [name, index, filename] = entry.find(TAG_OBJECT_NAME, TAG_INDEX, TAG_FILE_NAME);

		// Part needs to have at least a valid object name and filename pointing to embedded .3db
		if (! name) throw new RangeError("Compound part (" + tag + ") is missing object name");
		if (! filename) throw new RangeError("Compound part (" + tag + ") is missing fragment file name");

		name = name.data.readString();
		filename = filename.data.readString();
		if (index) [index] = index.data.readInt32(1);

		// Find referenced filename in fragments
		let [fragment] = fragments.find(filename);
		if (! fragment) throw new RangeError("Referenced fragment entry (" + filename + ") for part (" + name + ") is missing");
		if (! fragment.hasChild) throw new RangeError("Fragment entry is empty");
		
		let part = new constructor();
		if (typeof part.load == "function") part.load(fragment);

		yield [name, index, fragment, part];
	}
}