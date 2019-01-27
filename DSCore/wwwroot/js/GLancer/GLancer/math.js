/**
 * Adds extra functions to compliment glMatrix library
 */
import {mat4} from "../glmatrix/index.js";

export function lerp(a, b, t) {
	return a * (1 - t) + b * t;
}

/**
 * Creates mat4 from position vector and transposed rotation matrix
 * Use for compound model constraints and hardpoints
 * 
 * @param  {mat4} out Receiving matrix
 * @param  {vec3} p   Position vector
 * @param  {mat3} r   Transposed rotation matrix
 * @return {mat4} out
 */
export function mat4fromPositionRotation(out, p, r) {

	// Copy position and transposed rotation matrix
	out[0]  = r[0]; out[1]  = r[3]; out[2]  = r[6]; out[3]  = 0;
	out[4]  = r[1]; out[5]  = r[4]; out[6]  = r[7]; out[7]  = 0;
	out[8]  = r[2]; out[9]  = r[5]; out[10] = r[8]; out[11] = 0;
	out[12] = p[0]; out[13] = p[1]; out[14] = p[2]; out[15] = 1;
	return out;
}

/**
 * Creates quat from pitch and yaw for first personal camera
 *
 * @param  {quat}   out Receiving quaternion
 * @param  {Number} p   Pitch (angle)
 * @param  {Number} y   Yaw (angle)
 * @return {quat}   out
 */
export function quatFromPitchYaw(out, p, y) {
	let halfToRad = 0.5 * Math.PI / 180.0;
	p *= halfToRad;
	y *= halfToRad;

	let sp = Math.sin(p),
		cp = Math.cos(p),
		sy = Math.sin(y),
		cy = Math.cos(y);

	out[0] = sp * cy;
	out[1] = cp * sy;
	out[2] = sp * sy;
	out[3] = cp * cy;

	return out;
}

/**
 * Creates mat4 from pitch and yaw
 *
 * @param  {mat4}   out Receiving matrix
 * @param  {Number} p   Pitch (radians)
 * @param  {Number} y   Yaw (radians)
 * @return {mat4}   out
 */
export function mat4fromPitchYaw(out, p, y) {
	mat4.identity(out);
	mat4.rotateX(out, out, p + Math.PI);
	mat4.rotateY(out, out, y);
	mat4.rotateZ(out, out, -Math.PI);
	return true;
}