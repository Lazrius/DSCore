import {Glancer} from "./GLancer/system.js";
import {ShowRoomScene} from "./GLancer/scene.js";

const viewport = document.getElementById("model"),
	zoom = document.getElementById("zoom");

Promise.all([
	Glancer.resources.getModel(viewport.getAttribute("file")),
	Glancer.resources.getResources(viewport.getAttribute("mat")),
	Glancer.resources.loadShadersXML("shaders.xml")
]).then(([
	model
]) => {
	let loop = Glancer.createLoop(),
		range;

	loop.render.initialize(viewport, true, true);
	loop.render.alpha = 0;
	loop.scene = new ShowRoomScene(model);

	range = loop.scene.camera.position[2];

	zoom.oninput = event => loop.scene.camera.position[2] = range * event.target.value;
	loop.start();
});