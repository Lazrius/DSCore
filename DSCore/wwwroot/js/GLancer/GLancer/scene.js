import {quat, vec3, mat3, mat4} from "../GLmatrix/index.js";
import {SimpleRigidModel, CompoundRigidModel} from "./rigid.js";

/**
 * A simple scene object without any form visualization of its own
 *
 * @property {Float32Array} position Position vector
 * @property {Float32Array} rotation Rotation quaternion
 */
export class SceneObject {
	constructor() {
		this.position = vec3.create();
		this.rotation = quat.create();
		this.rotationInverse = quat.create();
		this.step = vec3.create();
	}

	rotate(q) {
		quat.copy(this.rotation, q);
	}

	move(v) {
		vec3.scaleAndAdd(this.position, this.position, v, 0.5);
		//vec3.add(this.position, this.position, v);
	}

	moveLocal(v) {
		quat.invert(this.rotationInverse, this.rotation);
		vec3.transformQuat(this.step, v, this.rotation);
		vec3.add(this.position, this.position, this.step);
	}

	update(timeDelta) {

	}
}

export const lightTypes = Object.create(null);

lightTypes.DIRECTIONAL = 0;
lightTypes.SPOT        = 1;
/**
 * @property {Float32Array} color  Light color
 * @property {Number}       radius Light radius
 */
export class LightSource extends SceneObject {
	constructor() {
		super();

		this.color = vec3.create();
		this.range = 1000;
		this.type  = lightTypes.DIRECTIONAL;
	}
}

export class PerspectiveCameraObject extends SceneObject {
	constructor(fov = 60 * Math.PI/180, near = 1.0, far = 10000.0) {
		super();

		this.fov  = fov;
		this.near = near;
		this.far  = far;
	}



	update(timeDelta) {
	}
}

/**
 * Scene object visualized by rigid mesh (simple or compound). Can have
 * attachments.
 *
 * @property {RigidModel} model
 */
export class RigidObject extends SceneObject {
	constructor(model) {
		super();

		if (! (model instanceof SimpleRigidModel || model instanceof CompoundRigidModel)) throw new TypeError("Invalid rigid model type");

		this.model = model; // SimpleRigidModel/CompoundRigidModel
		this.attachments = new Map(); // Attachment -> Hardpoint
	}

}

/**
 * A static scene object
 */
export class SolarObject extends RigidObject {
	constructor(model) {
		super(model);

		this.health = 100;
		this.destructible = false;
	}

}

/**
 * Star system, space scene
 *
 * @property {String}             title   Display name
 * @property {Float32Array}       color   Space color ([SystemInfo]->space_color)
 * @property {CompoundRigidModel} nebulae Background nebula model ([Background]->nebulae)
 * @property {CompoundRigidModel} stars   Background stars model ([Background]->complex_stars)
 * @property {Float32Array}       ambient Default ambient color
 * @property {Map}                objects Solar objects ([Object])
 * @property {Map}                lights  Light sources ([LightSource])
 * @property {Map}                zones   Zones ([Zone])
 * @property {Camera}             camera  Active camera object which will be used to render the scene
 * @property {Number}             elapsed Time elapsed in space system
  */
export class SpaceScene {
	constructor(title) {
		this.title   = title;
		this.color   = new Float32Array(3);
		this.nebulae = undefined;
		this.stars   = undefined;
		this.ambient = new Float32Array(3);
		this.objects = new Map();
		this.lights  = new Map();
		this.zones   = new Map();
		this.camera  = new PerspectiveCameraObject();
		this.elapsed = 0;
	}

	*getLights() {
		for (let light of this.lights.entries()) {
			yield light;
		}
	}

	/**
	 * Get objects within distance to camera
	 * @return {[type]} [description]
	 */
	*getObjects() {
		for (let object of this.objects.entries()) {
			// TODO: filter objects by distance
			// TODO: filter objects by camera cone
			// TODO: Add light sources to the returning list


			yield object;
		}
	}

	update(timeDelta) {
		this.elapsed += timeDelta;

		if (this.camera) this.camera.update(timeDelta);

		// Propagate updates to all objects in scene
		for (let object of this.objects.values()) object.update(timeDelta);
	}
}

export class ShowRoomScene {
	constructor(model) {
		this.color   = new Float32Array(3);
		this.target  = new RigidObject(model);
		this.ambient = new Float32Array(3);
		this.camera  = new PerspectiveCameraObject();
		this.lights  = new Map();
		this.objects = new Map();
		this.elapsed = 0;

		this.ambient.fill(0.125);

		this.objects.set("ShowRoomSubject", this.target);
		this.camera.position[2] = -model.getRadius();

		let lightTop = new LightSource();

		this.lights.set("ShowRoomLight", lightTop);
		lightTop.position[0] = 4000;
		lightTop.position[1] = 4000;
		lightTop.color.fill(1);
	}

	*getLights() {
		for (let light of this.lights.entries()) yield light;
	}

	*getObjects() {
		yield this.target;
	}

	update(timeDelta) {
		this.elapsed += timeDelta;

		quat.rotateY(this.target.rotation, this.target.rotation, 0.25 * timeDelta / 1000);
	}
}