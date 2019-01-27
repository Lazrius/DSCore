import {UTFReader, UTFEntry} from "./utf.js";

/**
 * @property {String} vertex   Vertex shader source code
 * @property {String} fragment Fragment shader source code
 * @property {Number} version  GLES version
 */
export class Shader {
	constructor() {
		this.vertex   = "";
		this.fragment = "";
		this.version  = 0; // GLES version
	}

	/**
	 * Load shader source from UTF folder
	 * @param  {UTFEntry} folder
	 * @return {Boolean}
	 */
	load(folder) {
		if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid UTF entry object");
		if (! folder.hasChild) return false;

		let [vertexShader, fragmentShader, version] = folder.find("vertexshader", "fragmentshader", "version");
		if (! vertexShader || ! fragmentShader) return false;

		this.vertex   = vertexShader.data.readString();
		this.fragment = fragmentShader.data.readString();
		if (version) [this.version] = version.data.readFloat32(1);

		return true;
	}

	/**
	 * Load shader source from XML element
	 * @param  {Element} element
	 * @return {Boolean}
	 */
	loadXML(element) {
		if (! (element instanceof Element)) throw new TypeError("Invalid XML element object");
		if (! element.hasChildNodes()) return false;

		let vertexShader, fragmentShader, version;

		for (let property of element.children) switch (property.nodeName) {
			case "vertex-shader":   vertexShader   = property; break;
			case "fragment-shader": fragmentShader = property; break;
			case "version":         version        = property; break;
		}

		if (! vertexShader || ! fragmentShader) return false;

		this.vertex   = vertexShader.textContent;
		this.fragment = fragmentShader.textContent;
		if (version) this.version = parseFloat(verison.textContent);

		return true;
	}
}

/**
 * Load shader from UTF folder
 * @param  {UTFEntry} folder
 * @return {Shader}
 */
export function loadShader(folder) {
	let shader = new Shader();
	shader.load(folder);
	return shader;
}