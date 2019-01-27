/**
 * Similar to rigid mesh data shapes are only contain data to be drawn with
 * lines. As such there are only element indices, vertex position and optional
 * color buffers. Instead of mesh groups there are individual splines.
 */
import {UTFEntry} from "./utf.js";

export const types = Object.create(null);

types.LINES = 0; // gl.LINES
types.STRIP = 1; // gl.LINE_STRIP
types.LOOP  = 2; // gl.LINE_LOOP

const TAG_SPLINES = "splines",
	TAG_INDICES   = "indices",
	TAG_POSITIONS = "positions",
	TAG_COLORS    = "colors";

export class ShapeSpline {
	constructor() {
		this.color = new Float32Array(4);
		this.type  = types.LINES;
	}
}

/**
 * Shapes are meshes drawn with lines
 *
 * @property {Array}        splines   Sequence of lines
 * @property {Uint16Array}  indices   Line indices
 * @property {Float32Array} positions Positions
 * @property {Float32Array} colors    Colors
 */
export class ShapeMeshData {
	constructor() {
		this.splines   = [];
		this.indices   = undefined;
		this.positions = undefined;
		this.colors    = undefined;
	}

	load(folder) {
		if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object");
		if (! folder.hasChild) throw new RangeError("Shape folder is empty");

		let [splines, indices, positions, colors] = folder.find(TAG_SPLINES, TAG_INDICES, TAG_POSITIONS, TAG_COLORS);

		if (! splines) throw new Error("Shape is missing splines entry");
		if (! indices) throw new Error("Shape is missing indices entry");
		if (! positions) throw new Error("Shape is missing positions entry");

		this.indices   = indices.data.readInt16();
		this.positions = positions.data.readFloat32();

		if (colors) this.colors = colors.data.readInt8();
		return true;
	}
}

export function loadShape(folder) {
	let shape = new ShapeMeshData();
	shape.load(folder);
	return shape;
}

function getCirclePoints(segments = 4, radius = 1) {
	const c = (90 / segments) * Math.PI/180;
		x = [];
		y = [];

	for (let i = 0; i < segments; i++) {
		x[i] = Math.sin(i * c) * radius;
		y[i] = Math.cos(i * c) * radius;
	}

	return [x, y];
}

/**
 * Generates mirroerd cone marker to be drawn with gl.LINE_LOOP
 * @param  {Number} sides  Numbe of sides, must be even and no less than four
 * @param  {Number} height Cone hight
 * @param  {Number} radius Cone radius
 * @return {VMeshData}
 */
export function generateMarker(sides = 6, height = 1, radius = height * .5) {
	if ((sides % 2) != 0) throw RangeError("Amount of sides must be even");
	if (sides < 4) throw RangeError("Amount of sides must be minimum four");

	const shape = new ShapeMeshData();

	const c = (360 / sides) * Math.PI/180, // Angle
		p = new Float32Array((sides + 2) * 3);
		i = new Uint16Array(3 * sides);

	// First vertex is top, last vertex is bottom
	p[p.length - 2] = -(p[1] = height);

	// Generating circle points
	for (let s = 0; s < sides; s++) {
		p[3 + (s * 3)] = Math.sin(s * c) * radius;
		p[5 + (s * 3)] = Math.cos(s * c) * radius;
	}

	// Generating indices [a, ++n, ++n, z, ++n%s, --n]
	for (let k = 0, s = 0, d = sides + 1; k < i.length;) {
		i[k++] = 0;
		i[k++] = ++s;
		i[k++] = ++s;
		i[k++] = d;
		i[k++] = ++s % sides;
		i[k++] = --s;
	}

	shape.groups.push(new ShapeSpline(i.length, types.LOOP));
	shape.indices   = i;
	shape.positions = p;

	return shape;
}