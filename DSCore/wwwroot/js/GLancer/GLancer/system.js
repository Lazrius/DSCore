import {TimeWatch} from "./core.js";
import {FileLoader} from "./loader.js";
import {Resources} from "./resource.js";
import {Render} from "./render.js";

/**
 *
 * @property {FileLoader} loader    Unified external assets loader
 * @property {Resources}  resources Resources library
 */
class GlancerEngine {
	constructor() {
		this.loader    = new FileLoader();
		this.resources = new Resources();
	}

	createLoop(canvas) {
		return new Loop(canvas);
	}
}

export const Glancer = new GlancerEngine();

/**
 * Independent loop
 *
 * Each loop will form its own rendering context as single context cannot be
 * shared between multiple canvas elements.
 * 
 * @property {Render}    render Target render
 * @property {TimeWatch} watch  Loop watch
 * @property {Number}    frame  Scheduled animation frame
 */
class Loop {

	/**
	 * @param  {HTMLCanvasElement} canvas
	 */
	constructor(canvas) {
		this.status     = Loop.STOPPED;
		this.render     = new Render();
		this.controller = undefined;
		this.scene      = undefined;
		this.frame      = 0;
		this.watch      = undefined;
	}

	setController(controller) {
		if (this.controller) this.controller.releaseControl();

		this.controller = controller;
		return controller.requestControl(this.render.context.canvas);
	}

	/**
	 * Start loop
	 * @return {Boolean}
	 */
	start() {	
		if (this.frame > 0) this.stop();

		this.watch = new TimeWatch();
		let controllerPosition;

		const frameLoop = timeNow => {
			this.watch.tick(timeNow);

			if (this.controller) {
				this.status = Loop.CONTROLLER;
				this.controller.update(this.watch.delta);
				controllerPosition = this.controller.position;
			} else controllerPosition = undefined;

			if (this.scene) {
				this.status = Loop.SCENE;
				this.scene.update(this.watch.delta);

				if (this.render) {
					this.status = Loop.RENDER;

					if (! this.render.drawScene(this.watch.delta, this.scene, controllerPosition))
						return false;
				}
			}

			this.status = Loop.REQUEST;
			this.frame  = requestAnimationFrame(frameLoop, this.render.canvas);
		};

		this.frame = requestAnimationFrame(frameLoop, this.render.canvas);
		return true;
	}

	/**
	 * Stop loop
	 * @return {Boolean}
	 */
	stop() {
		if (this.frame == 0) return false;

		cancelAnimationFrame(this.frame);
		this.frame = 0;
		return true;
	}
}

Loop.STOPPED    = 0;
Loop.CONTROLLER = 1;
Loop.SCENE      = 2;
Loop.RENDER     = 3;
Loop.REQUEST    = 4;
