/**
 * Models have hardpoints to which other objects can be attached, such as
 * equipment hardware, visual particle effects, etc.
 *
 * Three types are available in Freelancer: fixed, revolute and prismatic.
 * Although only fixed and revoute hardpoints are used in game.
 *
 * Each hardpoint may have more than one attachment. 
 */

import {UTFEntry} from "./utf.js";
import {mat4fromPositionRotation} from "./math.js";

/**
 * UTF entry tags corresponding types
 * @type {String}
 */
const TAG_HARDPOINTS = "hardpoints",
	TAG_FIXED        = "fixed",
	TAG_REVOLUTE     = "revolute",
	TAG_PRISMATIC    = "prismatic",
	TAG_POSITION     = "position",
	TAG_ORIENTATION  = "orientation",
	TAG_AXIS         = "axis",
	TAG_MIN          = "min",
	TAG_MAX          = "max";

/**
 * Load hardpoints
 * @param {UTFEntry} folder Entry containing `Hardpoints`
 * @yield {[name, Hardpoint]}
 */
export function* loadHardpoints(folder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object type");
	if (! folder.hasChild) return false;

	let [hardpoints] = folder.find(TAG_HARDPOINTS);
	if (! hardpoints) return false;

	let [fixed, revolute, prismatic] = hardpoints.find(TAG_FIXED, TAG_REVOLUTE, TAG_PRISMATIC);

	if (fixed)     yield* createHardpoint(fixed,     FixedHardpoint);
	if (revolute)  yield* createHardpoint(revolute,  RevoluteHardpoint);
	if (prismatic) yield* createHardpoint(prismatic, PrismaticHardpoint);
}

/**
 * Create hardpoint
 * @param {UTFEntry} folder      `Fixed` or `Revolute` or `Prismatic` entry
 * @param {function} constructor Hardpoint constructor
 * @yield {[string, Hardpoint]}
 */
function* createHardpoint(folder, constructor) {
	for (let [tag, entry] of folder) {
		let hardpoint = new constructor();
		hardpoint.load(entry, tag);
		yield [entry.name, hardpoint];
	}
}

export class FixedHardpoint {
	constructor() {
		this.transform = new Float32Array(16); // Transformation matrix
	}

	load(folder, name) {
		if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object type");
		if (! folder.hasChild) return false;

		let [position, orientation] = folder.find(TAG_POSITION, TAG_ORIENTATION);

		if (! position) throw new RangeError("Hardpoint " + name + " is missing position entry");
		if (! orientation) throw new RangeError("Hardpoint " + name + " is missing orientation entry");

		mat4fromPositionRotation(this.transform, position.data.readFloat32(3), orientation.data.readFloat32(9));
	}
}

export class RevoluteHardpoint extends FixedHardpoint {
	constructor() {
		super();

		this.axis = new Float32Array(3);
		this.min  = 0;
		this.max  = 0;
	}

	load(folder, name) {
		super.load(folder, name);

		let [axis, min, max] = folder.find(TAG_AXIS, TAG_MIN, TAG_MAX);

		if (! axis) throw new RangeError("Hardpoint " + name + " is missing axis entry");
		if (! min) throw new RangeError("Hardpoint " + name + " is missing min entry");
		if (! max) throw new RangeError("Hardpoint " + name + " is missing max entry");

		this.axis = axis.data.readFloat32(3);
		
		[this.min] = min.data.readFloat32(1);
		[this.max] = max.data.readFloat32(1);
		return true;
	}
}

export class PrismaticHardpoint extends RevoluteHardpoint {}