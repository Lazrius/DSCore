/**
 * Constraints are used by compound models (see compound.js) to establish
 * constraining link between child and parent parts of a model. Freelancer
 * supports the following types of constraints: fixed (fix), revolute (rev),
 * prismatic (pris), cylindric (cyl), spherical (sphere), loose (loose).
 *
 * Presumably at one point other types existed, like damped spring.
 *
 * Constraints are typically loaded from Cmpnd\Cons\* UTF entries.
 */
import {UTFEntry} from "./utf.js";
import {mat4fromPositionRotation} from "./math.js";

/**
 * UTF entry tags corresponding types
 * @type {String}
 */
const TAG_CONS    = "cons",
	TAG_FIXED     = "fix",
	TAG_REVOLUTE  = "rev",
	TAG_PRISMATIC = "pris",
	TAG_CYLINDRIC = "cyl",
	TAG_SPHERICAL = "sphere",
	TAG_LOOSE     = "loose";

/**
 * Byte length of each constraint type. Constraints of single type packed
 * together into single UTF entry data.
 * @type {Number}
 */
const LENGTH_FIXED   = 0xB0, // 176
	LENGTH_REVOLUTE  = 0xD0, // 208
	LENGTH_PRISMATIC = 0xD0, // 208
	LENGTH_CYLINDRIC = 0xCC, // 204
	LENGTH_SPHERICAL = 0xD4, // 212
	LENGTH_LOOSE     = 0xB0; // 176

/**
 * Get constraints from `Cmpnd\Cons`
 * @param {UTFEntry} folder `Cmpnd` entry
 * @yield {[string, string, Constraint]}
 */
export function* loadConstraints(folder, skeleton = false) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object type");
	if (! folder.hasChild) throw new TypeError("Constraints entry is empty");

	// Check for `cons` entry in compound
	let [constraints] = folder.find(TAG_CONS);
	if (! constraints) return false;

	// Find all types
	let [fixed, revolute, prismatic, cylindric, spherical, loose] = constraints.find(TAG_FIXED, TAG_REVOLUTE, TAG_PRISMATIC, TAG_CYLINDRIC, TAG_SPHERICAL, TAG_LOOSE);

	if (fixed)     yield* createConstraint(fixed.data,     FixedConstraint,     LENGTH_FIXED);
	if (revolute)  yield* createConstraint(revolute.data,  RevoluteConstraint,  LENGTH_REVOLUTE);
	if (prismatic) yield* createConstraint(prismatic.data, PrismaticConstraint, LENGTH_PRISMATIC);
	if (cylindric) yield* createConstraint(cylindric.data, CylindricConstraint, LENGTH_CYLINDRIC);
	if (spherical) yield* createConstraint(spherical.data, SphericalConstraint, LENGTH_SPHERICAL);
	if (loose)     yield* createConstraint(loose.data,     LooseConstraint,     LENGTH_LOOSE);
}

/**
 * Get constraints from data of `Cmpnd\Cons\*`
 * @param {ArrayBufferWalker} data
 * @param {function}          constructor Constraint constructor
 * @param {Number}            length      Constraint size
 * @yield {[string, string, Constraint]}
 */
function* createConstraint(data, constructor, length) {
	let constraint, parent, child, bone;

	while (data.byteLength >= length) {
		parent = data.readString(0x40); // 64 bytes
		child  = data.readString(0x40); // 64 bytes

		(constraint = new constructor()).load(data);
		yield [child, parent, constraint];
	}
}

/**
 * Fixed constraint just provides transformation matrix, position and rotation
 * from parent part. No motion allowed. Cannot be animated. Treated specially in
 * hitboxes.
 */
export class FixedConstraint {
	constructor() {
		this.transform = new Float32Array(16); // 4x4 matrix
	}

	load(data) {
		let position = data.readFloat32(3),
			rotation = data.readFloat32(9);

		mat4fromPositionRotation(this.transform, position, rotation);
	}
}

/**
 * Treated as moveable part, but has no constraining motion.
 * Animation keyframe is complex matrix.
 */
export class LooseConstraint extends FixedConstraint {}

/**
 * Revolute constraint for swinging animation. Rotation across single axis
 * between two angles.
 *
 * Animation keyframe is single float (vec1).
 */
export class RevoluteConstraint {
	constructor() {
		this.transform = new Float32Array(16);
		this.offset    = new Float32Array(3);
		this.axis      = new Float32Array(3);
		this.min       = 0;
		this.max       = 0;
	}

	load(data) {
		let position = data.readFloat32(3), // Position
			offset   = data.readFloat32(3), // Offset
			rotation = data.readFloat32(9), // Rotation matrix
			axis     = data.readFloat32(3), // Animation axis
			limits   = data.readFloat32(2); // Animation limits

		mat4fromPositionRotation(this.transform, position, rotation);

		this.offset = offset.slice();
		this.axis   = axis.slice();
		this.min    = limits[0];
		this.max    = limits[1];
	}
}

/**
 * Prismatic constraint for sliding animation. Translation across single axis
 * between two values. Animation keyframe is single float (vec1).
 */
export class PrismaticConstraint extends RevoluteConstraint {}

/**
 * Cylindric combines both prismatic and revolution constraints. Translation
 * across single axis and roll across the same. Independent limits for rotation
 * and translation. Animation keyframe is two floats (vec2).
 */
export class CylindricConstraint {
	constructor() {
		this.transform = new Float32Array(16);
		this.axis      = new Float32Array(3);
		this.min       = new Float32Array(2);
		this.max       = new Float32Array(2);
	}

	load(data) {
		let position = data.readFloat32(3),
			rotation = data.readFloat32(9),
			axis     = data.readFloat32(3),
			limits   = data.readFloat32(4);

		mat4fromPositionRotation(this.transform, position, rotation);

		this.axis   = axis.slice();
		this.min[0] = limits[0];
		this.min[1] = limits[2];
		this.max[0] = limits[1];
		this.max[1] = limits[3];
	}
}

/**
 * Spherical constraint. Animation keyframe is quaternion (vec4/quat).
 */
export class SphericalConstraint {
	constructor() {
		this.transform = new Float32Array(16);
		this.offset    = new Float32Array(3);
		this.min       = new Float32Array(3);
		this.max       = new Float32Array(3);
	}

	load(data) {
		let position = data.readFloat32(3),
			offset   = data.readFloat32(3),
			rotation = data.readFloat32(9),
			limits   = data.readFloat32(6);

		mat4fromPositionRotation(this.transform, position, rotation);

		this.offset = offset.slice();
		this.min[0] = limits[0];
		this.min[1] = limits[2];
		this.min[2] = limits[4];
		this.max[0] = limits[1];
		this.max[1] = limits[3];
		this.max[2] = limits[5];
	}
}