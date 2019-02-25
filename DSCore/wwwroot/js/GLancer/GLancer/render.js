/**
 * While WebGL2 simplifies a few things and offers new features it isn't widely
 * available yet, so render uses standard WebGLRenderingContext with only
 * extension requirement being OES_vertex_array_object, which is as common as
 * WebGL1 support nowadays. If need be there's even Khronos Group polyfill for
 * vertex array objects.
 *
 * However VMeshData cannot be used "as-is" because indices elements buffer are
 * relative and need base index and vertex (which are provided by VMeshRef).
 * Unfortunately WebGL has no support for ARB_draw_elements_base_vertex
 * extension. Upon drawing model element indices are modified to absolute values
 * at every VMeshGroup referenced in VMeshRef. Indices are modified directly in
 * element buffer of vertex array object and it is done only once.
 */
import {vec2, vec3, vec4, mat3, mat4} from "../GLmatrix/index.js";
import {getRangeIndex} from "./core.js";
import {Glancer} from "./system.js";
import {UTFReader, UTFEntry} from "./utf.js";
import {types as textureTypes, Texture} from "./texture.js";
import {decompressDXT} from "./dxt.js";
import {Material, NebulaMaterial} from "./material.js";
import {Shader} from "./shader.js";
import {modes, VMeshData, VMeshRef} from "./vmesh.js";
import {types as shapeTypes, ShapeMeshData} from "./shape.js";
import {RigidObject} from "./scene.js";

const RENDER_WEBGL_CONTEXT       = "experimental-webgl",
	RENDER_WEBGL2_CONTEXT        = "webgl2",
	EXTENSION_ANISOTROPIC_FILTER = "EXT_texture_filter_anisotropic",
	EXTENSION_VERTEX_ARRAY       = "OES_vertex_array_object",
	EXTENSION_S3TC               = "WEBGL_compressed_texture_s3tc";

export const status = Object.create(null);

status.UNINITIALIZED = 0; // Object created but no context yet
status.INITIALIZED   = 1; // Target canvas initialized with context
status.STARTED       = 2; // Render has started to draw frame
status.BACKDROP      = 3; // Drawing backdrop objects
status.OBJECTS       = 4; // Drawing scene objects
status.OVERLAYS      = 5; // Drawing overlays
status.FINISHED      = 10; // Render has finished drawing frame

const mapModes = Object.create(null);

mapModes.MAP_DIFFUSE  = 1;
mapModes.MAP_EMISSION = 2;
mapModes.MAP_DETAIL   = 4;

const WHITE = vec3.create();
WHITE.fill(1);

export const DRAW_STARS = 1,
	DRAW_NEBULAE = 2,
	DRAW_OBJECTS = 4,
	DRAW_EFFECTS = 8,
	DRAW_HELPERS = 16,
	DRAW_OVERLAY = 32,
	DRAW_TEXTURES = 64,
	DRAW_LIGHTING = 128;

/**
 * Create and link program from shaders
 * @param  {WebGLRenderingContext} gl
 * @param  {String} vertexShaderSource
 * @param  {String} fragmentShaderSource
 * @return {WebGLProgram}
 */
function createProgram(gl, vertexShaderSource, fragmentShaderSource) {
	if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");

	/**
	 * Create and compile shader from source
	 * @param  {String} source
	 * @param  {Number} type
	 * @return {WebGLShader}
	 */
	function createShader(source, type) {
		const shader = gl.createShader(type);

		gl.shaderSource(shader, source);
		gl.compileShader(shader);

		if (! gl.getShaderParameter(shader, gl.COMPILE_STATUS) && ! gl.isContextLost())
			throw new Error("Error compiling shader:\n" + gl.getShaderInfoLog(shader));

		return shader;
	}

	const vertexShader = createShader(vertexShaderSource, gl.VERTEX_SHADER),
		fragmentShader = createShader(fragmentShaderSource, gl.FRAGMENT_SHADER),
		program        = gl.createProgram();

	gl.attachShader(program, vertexShader);
	gl.attachShader(program, fragmentShader);
	gl.linkProgram(program);

	if (! gl.getProgramParameter(program, gl.LINK_STATUS) && ! gl.isContextLost())
		throw new Error("Error linking program:\n" + gl.getProgramInfoLog(program));

	return program;
}

/**
 * Create WebGLTexture from Texture
 * @param  {WebGLRenderingContext} gl
 * @param  {Texture}               texture
 * @param  {Boolean}               alpha   Use alpha channel (from material OcOt)
 * @return {WebGLTexture}
 */
function createTexture(gl, texture, alpha) {
	if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
	if (! (texture instanceof Texture)) throw new TypeError("Invalid texture object");

	// Validate texture
	if (! texture.mipmaps.length) throw new RangeError("Texture object has no mipmaps");

	let format,	type, decompressor;

	// Texture types
	if (! format) switch (texture.type) {
		case textureTypes.RGB16_565:   format = gl.RGB;  type = gl.UNSIGNED_SHORT_5_6_5; break;
		case textureTypes.RGBA16_4444: format = gl.RGBA; type = gl.UNSIGNED_SHORT_4_4_4_4; break;
		case textureTypes.RGBA16_5551: format = gl.RGBA; type = gl.UNSIGNED_SHORT_5_5_5_1; break;
		case textureTypes.RGB24_888:   format = gl.RGB;  type = gl.UNSIGNED_BYTE; break;
		case textureTypes.RGBA32_8888: format = gl.RGBA; type = gl.UNSIGNED_BYTE; break;
		case textureTypes.S3TC_DXT1:
		case textureTypes.S3TC_DXT3:
		case textureTypes.S3TC_DXT5:   format = alpha ? gl.RGBA : gl.RGB; type = gl.UNSIGNED_BYTE; decompressor = decompressDXT; break;
		default: throw new TypeError("Unknown texture type");
	}

	// Create texture buffer
	const buffer = gl.createTexture();

	// Bind texture buffer
	gl.bindTexture(gl.TEXTURE_2D, buffer);

	// Setup filters
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

	// Buffer mipmaps
	for (let [level, width, height, mipmap] of texture.getMipmaps()) {
		if (level > 0) break;
		if (typeof decompressor == "function") mipmap = decompressor(mipmap, width, height, texture.type, alpha);
		gl.texImage2D(gl.TEXTURE_2D, level, format, width, height, 0, format, type, mipmap);
	}

	// Generate remaining mipmaps
	gl.generateMipmap(gl.TEXTURE_2D);

	// Unbind texture to prevent accidental modifications
	gl.bindTexture(gl.TEXTURE_2D, null);
	return buffer;
}

/**
 * Create WebGLVertexArrayObject from VMeshData
 * @param  {WebGLRenderingContext} gl
 * @param  {VMeshData}             mesh
 * @return {WebGLVertexArrayObject}
 */
function createVertexArray(gl, mesh) {
	if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
	if (! (mesh instanceof VMeshData)) throw new TypeError("Invalid mesh data object");

	// Validate mesh data
	if (! mesh.groups.length)   throw new RangeError("Mesh data has no groups");
	if (! mesh.indices.length)  throw new RangeError("Mesh data has no indices");
	if (! mesh.vertices.length) throw new RangeError("Mesh data has no vertices");

	// Get vertex array extension
	const vao = gl.getExtension(EXTENSION_VERTEX_ARRAY);
	
	// Create vertex array object and buffers
	const vertexArray = vao.createVertexArrayOES(),
		indexBuffer   = gl.createBuffer(),
		vertexBuffer  = gl.createBuffer();

	// Attribute index position
	let position = 0;

	// Bind vertex array object
	vao.bindVertexArrayOES(vertexArray);

	// Bind and buffer index data
	gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
	gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, mesh.indices, gl.STATIC_DRAW);

	// Bind and buffer interleaved vertex data
	gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
	gl.bufferData(gl.ARRAY_BUFFER, mesh.vertices, gl.STATIC_DRAW);

	// Setup position/normal/diffuse/specular attribute pointers
	if (mesh.positionOffset >= 0) {
		gl.vertexAttribPointer(0, 3, gl.FLOAT,         false, mesh.vertexLength, mesh.positionOffset);
		gl.enableVertexAttribArray(0);
	}

	if (mesh.normalOffset   >= 0) {
		gl.vertexAttribPointer(1, 3, gl.FLOAT,         false, mesh.vertexLength, mesh.normalOffset);
		gl.enableVertexAttribArray(1);
	}

	if (mesh.diffuseOffset  >= 0) {
		gl.vertexAttribPointer(2, 4, gl.UNSIGNED_BYTE, true,  mesh.vertexLength, mesh.diffuseOffset);
		gl.enableVertexAttribArray(2);
	}

	/*
	if (mesh.specularOffset >= 0) {
		gl.vertexAttribPointer(3, 4, gl.UNSIGNED_BYTE, true,  mesh.vertexLength, mesh.specularOffset);
		gl.enableVertexAttribArray(3);
	}
	*/

	// Setup texture mapping attribute pointers
	for (let i = 0; i < mesh.mapCount; i++) {
		gl.vertexAttribPointer(3 + i, 2, gl.FLOAT, false, mesh.vertexLength, mesh.getMapOffset(i));
		gl.enableVertexAttribArray(3 + i);
	}

	// Unbind Vertex Array Object to avoid accidental buffer modifications
	vao.bindVertexArrayOES(null);
	return vertexArray;
}

/**
 * Create WebGLVertexArrayObject from ShapeMeshData.
 * 
 * @param  {WebGLRenderingContext} gl
 * @param  {ShapeMeshData}          shape
 * @return {WebGLVertexArrayObject}
 */
function createShapeVertexArray(gl, shape) {
	if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
	if (! (shape instanceof ShapeMeshData)) throw new TypeError("Invalid mesh data object");

	// Validate shape data
	if (! shape.indices.length)   throw new RangeError("Shape mesh data has no indices");
	if (! shape.positions.length) throw new RangeError("Shape mesh data has no positions");
	
	// Get vertex array extension
	const vao = gl.getExtension(EXTENSION_VERTEX_ARRAY);

	// Create vertex array object and buffers
	const vertexArray  = vao.createVertexArrayOES(),
		indexBuffer    = gl.createBuffer(),
		positionBuffer = gl.createBuffer(),
		colorBuffer    = gl.createBuffer();

	// Bind vertex array object
	vao.bindVertexArrayOES(vertexArray);

	// Bind and buffer index data
	gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
	gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, shape.indices, gl.STATIC_DRAW);

	// Bind and buffer positions
	gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
	gl.bufferData(gl.ARRAY_BUFFER, shape.positions, gl.STATIC_DRAW);
	gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 0, 0);

	// Bind and buffer colors
	if (shape.colors instanceof Uint8Array && shape.colors.length > 0) {
		gl.bindBuffer(gl.ARRAY_BUFFER, colorBuffer);
		gl.bufferData(gl.ARRAY_BUFFER, shape.colors, gl.STATIC_DRAW);
		gl.vertexAttribPointer(0, 4, gl.UNSIGNED_BYTE, true, 0, 0);
	}

	// Unbind Vertex Array Object to avoid accidental buffer modifications
	vao.bindVertexArrayOES(null);
	return vertexArray;
}

/**
 * Render model is a cache created automatically for models
 *
 * @property {RigidModel} target        [description]
 * @property {WeakMap}    referenceData VMeshRef->VMeshData (straight to VAO?)
 * @property {WeakMap}    groupMaterial VMeshGroup->Material (to get textures?)
 * @property {WeakMap}    groupProgram  VMeshGroup->WebGLProgram (shader)
 * @property {Array}      opaques       Parts which contain only opaque mesh groups
 * @property {Array}      translucents  Parts which contain one or more translucent mesh groups

 */
class RenderModel {
	constructor(model) {
		this.target = model;

		this.referenceData = new WeakMap();
		this.groupMaterial = new WeakMap();
		this.groupProgram  = new WeakMap();
		this.opaques       = new Array();
		this.translucents  = new Array();

	}

}

/**
 * Render object is a cache created automatically for scene objects
 *
 * @property {SceneObject} target      Encapsulated target object
 * @property {mat4}        modelMatrix Scene model matrix
 */
class RenderObject extends RenderModel {

	constructor(object) {
		super(object.model);

		this.target      = object;
		this.modelMatrix = mat4.create();
	}
}

const MAX_LIGHTS = 6;

/**
 * Main render class.
 *
 * drawScene draws a frame of given scene with camera.
 * 
 * @property {WebGLRenderingContext} context        WebGL rendering context
 * @property {Number}                status         Current render status
 * @property {Number}                mode           Toggle things to render
 * @property {Number}                aspectRatio    Current viewport aspect ratio
 * @property {Number}                frameCount     Frames rendered
 * @property {Number}                drawCount      Draw calls count in last frame
 *
 * Quality adjustments
 * @property {Number}                viewportScale  Viewport scale to adjust for lower quality* 
 * @property {Number}                LODBias        LOD selection bias multiplier
 *
 * Association maps & lists
 * @property {WeakMap}               textures       Texture->WebGLTexture list
 * @property {Map}                   programs       String->WebGLProgram list
 * @property {WeakMap}               vertexArrays   VMeshData->WebGLVertexArrayObjectOES
 * @property {WeakSet}               meshGroups     VMeshGroup aligned indices
 * @property {WeakMap}               transforms     Object model matrices
 * @property {RenderMatrices}        matrices       Frame rendering matrices
 * @property {Float32Array}          ambientColor   Base ambient color
 * @property {Float32Array}          lightPositions Light positions buffer
 * @property {Float32Array}          lightColors    Light colors buffer
 */
export class Render {
	constructor() {
		this.context     = undefined;
		this.status      = status.UNINITIALIZED;
		this.mode        = DRAW_STARS | DRAW_NEBULAE | DRAW_OBJECTS | DRAW_EFFECTS | DRAW_OVERLAY | DRAW_TEXTURES | DRAW_LIGHTING;
		this.aspectRatio = 0;
		this.frameCount  = 0; // Frame draw count
		this.drawCount   = 0; // Draw call count in last frame

		this.viewportScale = 1.0;
		this.LODbias       = 1.0;

		this.backdropPosition = vec3.create();
		this.backdropScalar = 0;
		this.alpha          = 1;


		this.ambientColor   = vec3.create();
		this.highlightColor = vec3.create();
		this.lightPositions = new Float32Array(MAX_LIGHTS * 3);
		this.lightColors    = new Float32Array(MAX_LIGHTS * 3);
		this.lightCount     = 0;

		// TODO: Is it per part or per mesh group?

		this.opaques      = []; // Front to back opaques
		this.translucents = []; // Back to front translucents



		// Cursor handling
		this.cursor         = false;
		this.cursorPosition = vec2.create();

		this.cursorPixels = vec2.create(); // Pixel position of cursor
		this.targetPixels = vec2.create(); // Pixel position of target
	}

	/**
	 * Initialize rendering context for specified target canvas element
	 * @param  {HTMLCanvasElement} canvas     Target canvas element
	 * @param  {Boolean}           antialias  Enable built-in anti-alias
	 * @param  {Number}            anisotropy Anisotropy level
	 * @return {Boolean}
	 */
	initialize(canvas, antialias = false, alpha = false) {
		if (! (canvas instanceof HTMLCanvasElement)) throw new TypeError("Invalid HTML canvas element object");

		/** @type {WebGLRenderingContext} */
		const gl = canvas.getContext(RENDER_WEBGL_CONTEXT, {
			alpha: alpha,
			antialias: antialias,
			preserveDrawingBuffer: true
		});

		if (! gl) throw new Error("Unable to get WebGL rendering context");

		gl.enable(gl.DEPTH_TEST);
		gl.enable(gl.CULL_FACE);
		gl.enable(gl.BLEND);

		gl.cullFace(gl.BACK);		

		// Links between resource objects and their bufferized WebGL counterparts
		this.textures     = new WeakMap();
		this.programs     = new Map();
		this.vertexArrays = new WeakMap();
		this.meshGroups   = new WeakSet();
		this.objects      = new WeakMap();
		this.transforms   = new WeakMap();
		this.matrices     = new RenderMatrices();

		this.status       = status.INITIALIZED;

		this.context = gl;
		this.setupViewport();
		return true;
	}

	/**
	 * Setup viewport size and aspect ratio
	 * @return {Boolean}
	 */
	setupViewport() {
		const gl = this.context;
		if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");

		let style  = getComputedStyle(gl.canvas),
			width  = parseInt(style.getPropertyValue("width"), 10),
			height = parseInt(style.getPropertyValue("height"), 10);

		gl.canvas.width  = width * this.viewportScale;
		gl.canvas.height = height * this.viewportScale;

		gl.viewport(0, 0, gl.drawingBufferWidth, gl.drawingBufferHeight);
		this.aspectRatio = gl.canvas.clientWidth / gl.canvas.clientHeight;

		return true;
	}

	/**
	 * Create render object for scene object and store in Render.objects map
	 * @param  {SceneObject}  sceneObject
	 * @return {RenderObject}
	 */
	createRenderObject(sceneObject) {
		const renderObject = new RenderObject();



		this.objects.set(sceneObject, renderObject);
		return renderObject;
	}

	/**
	 * Get WebGLVertexArrayObject associated with VMeshData
	 * @param  {VMeshData} mesh
	 * @return {WebGLVertexArrayObject}
	 */
	getVertexArray(mesh) {
		if (! (mesh instanceof VMeshData)) throw new TypeError("Invalid mesh data object");
		if (this.vertexArrays.has(mesh)) return this.vertexArrays.get(mesh);

		const vertexArray = createVertexArray(this.context, mesh);
		this.vertexArrays.set(mesh, vertexArray);
		return vertexArray;
	}

	/**
	 * Delete WebGLVertexArrayObject associated with VMeshData
	 * @param  {VMeshData} mesh
	 * @return {Boolean}
	 */
	deleteVertexArray(mesh) {
		const gl = this.context,
			vao  = gl.getExtension(EXTENSION_VERTEX_ARRAY);

		if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
		if (! (mesh instanceof VMeshData)) throw new TypeError("Invalid mesh data object");
		if (! this.vertexArrays.has(mesh)) return false;

		const vertexArray = this.vertexArrays.get(mesh);
		if (vao.isVertexArrayOES(vertexArray)) vao.deleteVertexArrayOES(vertexArray);
		this.vertexArrays.delete(mesh);
		return true;
	}

	/**
	 * Get WebGLTexture associated with Texture
	 * @param  {Texture} texture
	 * @param  {Boolean} alpha   Texture contains alpha channel
	 * @return {WebGLTexture}
	 */
	getTexture(texture, alpha = false) {
		if (! (texture instanceof Texture)) throw new TypeError("Invalid texture object");
		if (this.textures.has(texture)) return this.textures.get(texture);

		const textureBuffer = createTexture(this.context, texture, alpha);
		this.textures.set(texture, textureBuffer);
		return textureBuffer;
	}

	/**
	 * Delete WebGLTexture associated with Texture
	 * @param  {Texture} texture
	 * @return {Boolean}
	 */
	deleteTexture(texture) {
		const gl = this.context;
		if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
		if (! (texture instanceof Texture)) throw new TypeError("Invalid texture object");
		if (! this.textures.has(texture)) return false;

		const textureBuffer = this.textures.get(texture);

		if (gl.isTexture(textureBuffer)) gl.deleteTexture(textureBuffer);
		this.textures.delete(texture);
		return true;
	}

	/**
	 * Get WebGLProgram associated with Material
	 * @param  {String} name
	 * @return {WebGLProgram}
	 */
	getProgram(name) {
		if (this.programs.has(name)) return this.programs.get(name);

		const shader = Glancer.resources.getShader(name);
		
		const program = createProgram(this.context, shader.vertex, shader.fragment);

		this.programs.set(name, program);
		return program;
	}

	/**
	 * Delete WebGLProgram associated with Material
	 * @param  {String} name
	 * @return {Boolean}
	 */
	deleteProgram(name) {
		const gl = this.context;
		if (! (gl instanceof WebGLRenderingContext)) throw new TypeError("Invalid rendering context object");
		if (! this.programs.has(name)) return false;

		const program = this.programs.get(name);

		if (gl.isProgram(program)) gl.deleteProgram(program);
		this.programs.delete(name);
		return true;
	}

	screenSpaceToPixels(out, v) {
		const gl = this.context,
			halfWidth = gl.drawingBufferWidth * .5,
			halfHeight = gl.drawingBufferHeight * .5;

		// this.position[0] = event.offsetX / (this.viewport.width * .5) - 1;
		// this.position[1] = 1 - event.offsetY / (this.viewport.height * .5);

		out[0] = Math.round(halfWidth * v[0] + halfWidth);
		out[1] = Math.round(halfHeight * v[1] + halfHeight);
	}

	/**
	 * Draw scene frame
	 * @param  {Number}  timeDelta Time between frame draw calls
	 * @param  {Scene}   scene     Scene to draw
	 * @param  {vec2}    position  Cursor position
	 * @return {Boolean} Must return true for next frame to be requested!
	 */
	drawScene(timeDelta, scene, position) {
		const gl = this.context;

		this.status = status.STARTED;

		// No scene to draw
		if (! scene) return false;

		// Mouse cursor
		if (position) {
			vec2.copy(this.cursorPosition, position);

			this.screenSpaceToPixels(this.cursorPixels, position);
			this.cursor = true;
		} else this.cursor = false;

		// Reset draw call counter
		this.drawCount = 0; 

		// Set scene clear
		gl.clearColor(scene.color[0], scene.color[1], scene.color[2], this.alpha);

		// Clear buffers
		gl.clear(gl.COLOR_BUFFER_BIT);
		gl.clear(gl.DEPTH_BUFFER_BIT);

		// Nothing to render if active camera is not specified
		// It isn't an error however
		if (! scene.camera) return true;

		gl.bindTexture(gl.TEXTURE_2D, null);

		// Setup camera perspective projection
		mat4.perspective(this.matrices.projection, scene.camera.fov, this.aspectRatio, scene.camera.near, scene.camera.far);

		// =====================================================================
		// BACKGROUNDS
		// =====================================================================
		this.status = status.BACKDROP;

		// Setup view matrix from camera orientation
		mat4.fromQuat(this.matrices.view, scene.camera.rotation);

		// Background parallax effect
		vec3.scale(this.backdropPosition, scene.camera.position, this.backdropScalar);
		mat4.translate(this.matrices.view, this.matrices.view, this.backdropPosition);

		// Backdrop has no ambient color (white)
		this.ambientColor.fill(1);
		this.lightCount = 0;

		// Draw stars and then nebula model
		if (this.mode & DRAW_STARS && scene.stars) this.drawRigidModel(scene.stars);
		if (this.mode & DRAW_NEBULAE && scene.nebulae) this.drawRigidModel(scene.nebulae);

		// Clear depth after backdrops are rendered
		gl.clear(gl.DEPTH_BUFFER_BIT);

		// TODO: Fog overlay for background as gl.clear with alpha relative to
		// position within zone border

		// Reset draw call counter
		//this.drawCount = 0; 

		// =====================================================================
		// SCENE OBJECTS
		// =====================================================================
		this.status = status.OBJECTS;

		// Translate view by camera position for scene objects
		mat4.translate(this.matrices.view, this.matrices.view, scene.camera.position);

		// Set ambient color for objects
		if (scene.ambient) vec3.copy(this.ambientColor, scene.ambient);

		// Setup lights
		for (let light of scene.lights.values()) {
			this.lightPositions.set(light.position, this.lightCount * 3);
			this.lightColors.set(light.color, this.lightCount * 3);
			this.lightCount++;
		}

		// Draw objects
		if (this.mode & DRAW_OBJECTS) for (let object of scene.objects.values()) {
			if (object instanceof RigidObject) this.drawRigidObject(object);

			// TODO: Drawing transparent objects back to front
		}

		// =====================================================================
		// OVERLAYS
		// =====================================================================
		this.status = status.OVERLAYS;

		// Frame rendering complete
		this.status = status.FINISHED;
		this.frameCount++;
		return true;
	}

	/**
	 * Get object transformation matrix
	 * @param  {Object} object
	 * @return {Float32Array}
	 */
	getObjectTransform(object) {
		// Get 4x4 transformation matrix for object
		let transform = this.transforms.get(object);

		// Create 4x4 transformation matrix for object if none exist
		if (! transform) this.transforms.set(object, transform = mat4.create());

		return transform;
	}

	/**
	 * Draw rigid object
	 * @param  {RigidObject} target
	 * @return {Boolean}
	 */
	drawRigidObject(object) {
		let transform = this.getObjectTransform(object);

		// Get object position and rotation
		mat4.fromRotationTranslation(transform, object.rotation, object.position);

		if (object.model) this.drawRigidModel(object.model, transform, object.ranges);
		return true;
	}

	/**
	 * Draw model parts
	 * @param  {CompoundRigidModel} model
	 * @param  {Float32Array}       transform 
	 * @param  {Array}              ranges    LOD override ranges
	 * @return {Boolean}
	 */
	drawRigidModel(model, transform, ranges) {
		let reference, depth, radius, highlight = false, inside = false, draw = true;

		for (let part of model.parts.values()) {
			highlight = false;

			mat4.identity(this.matrices.model); // Reset model matrix to object transform for next part
			this.stackTransforms(this.matrices.model, model, part); // Stack up part matrices to model matrix
			
			if (transform) mat4.multiply(this.matrices.model, transform, this.matrices.model);

			this.matrices.build(); // Build Model-View-Projection and Normal matrices

			depth = this.matrices.result[14]; //position[2];
			if (depth < 0) depth = 0; // Clamp at 0

			// Object LOD overrides
			if (ranges) reference = part.getLOD(getRangeIndex(depth, ranges, this.LODbias));
			else reference = part.getLODReference(depth);

			if (! reference) continue;
			
			if (this.status == status.OBJECTS) {
				draw = false;

				// Get camera space coordinates of bounding sphere center
				vec3.transformMat4(this.matrices.boundary2, reference.sphere.center, this.matrices.modelView);

				// Camera is inside bounding sphere
				inside = vec3.squaredLength(this.matrices.boundary2) <= reference.sphere.radius * reference.sphere.radius;
				if (inside) draw = true;

				// Get screen space coordinates of bounding sphere center
				vec3.transformMat4(this.matrices.boundary, reference.sphere.center, this.matrices.result);

				// Perspective correct radius?
				radius = Math.abs(reference.sphere.radius * (1 - this.matrices.boundary[2]));

				// Bounding sphere center is in screen
				if (this.matrices.boundary[2] < 1 && Math.abs(this.matrices.boundary[0]) < 1 + radius && Math.abs(this.matrices.boundary[1]) < 1 + radius) draw = true;

				// Problem with screen space coords is that they're square.
				this.screenSpaceToPixels(this.targetPixels, this.matrices.boundary);

				if (draw && this.cursor) {
					//if (vec2.squaredDistance(this.cursorPixels, this.targetPixels) < radius * radius) highlight = true;
					if (vec2.squaredDistance(this.cursorPosition, this.matrices.boundary) < radius * radius) highlight = true;
				}

				//if (draw && Math.abs(this.matrices.boundary[0]) < 0.1 && Math.abs(this.matrices.boundary[1]) < 0.1) highlight = true;

				// Culling out of frame parts
				// Figure out if it is in front or behind
				//if (boundaryX * boundaryX + boundaryY * boundaryY < radius * radius && this.matrices.boundary[2] < 1)
				
				// this.matrices.boundary[2] > 1
				// (Math.abs(this.matrices.boundary[0]) > 1 + radius || Math.abs(this.matrices.boundary[1]) > 1 + radius))
			}

			if (draw) this.drawRigidPart(part, reference, highlight); // Draw compound part mesh groups
		}

		return true;
	}

	/**
	 * Stack compound part constraint matrices
	 * 
	 * @param  {Float32Array} out   Model matrix
	 * @param  {RigidModel}   model Compound object
	 * @param  {RigidPart}    part  Compound part
	 * @return {Boolean}
	 */
	stackTransforms(out, model, part) {
		let parent = part;

		while (parent && parent != model.root) {
			let constraint = model.getConstraint(parent); // Transforms are stored in constraint data
			mat4.multiply(out, constraint.transform, out); // Multiply in reverse order
			if (constraint.offset) mat4.translate(out, out, constraint.offset); // Apply offset if constraint has it
			parent = model.getParent(parent); // Advance to part parent
		}

		return true;
	}

	/**
	 * Draw rigid part
	 * @param  {RigidPart} part      Compound rigid part
	 * @param  {VMeshRef}  reference VMeshData reference
	 * @return {Boolean}
	 */
	drawRigidPart(part, reference, highlight) {
		if (! reference.meshID) return false; // Mesh reference cannot be 0x0

		// TODO: Create Map for VMeshRef->VAO since meshID isn't supposed to be
		// a dynamic property. Cuts the resources library roundtrip.

		const gl = this.context,
			vao  = gl.getExtension(EXTENSION_VERTEX_ARRAY),
			mesh = Glancer.resources.getMesh(reference.meshID);

		if (! mesh) return false;

		/** @type {WebGLVertexArrayObject} VAO associated with meshID */
		const vertexArray = this.getVertexArray(mesh);

		/** @type {Number} Reference start index in VAO */
		let indexStart = reference.indexStart;

		// Bind vertex array object
		vao.bindVertexArrayOES(vertexArray);

		// Loop through VMeshGroups specified in reference
		for (let group of mesh.getGroupsByReference(reference)) {
			if (! this.meshGroups.has(group)) this.alignVertexArray(mesh, group, reference, indexStart);

			this.drawVMeshGroup(group, indexStart, highlight);
			indexStart += group.indexCount;
		}

		if (part.wireframe)

		return true;
	}

	/**
	 * Before drawing multi-part/multi-group model indices in its vertex array
	 * object must be recalculated to absolute values.
	 * 
	 * @param  {VMeshData}  mesh      Mesh data
	 * @param  {VMeshGroup} group     Mesh group
	 * @param  {VMeshRef}   reference [description]
	 * @return {Boolean}
	 */
	alignVertexArray(mesh, group, reference, indexStart) {
		const gl = this.context;

		let groupIndices = mesh.indices.slice(indexStart, indexStart + group.indexCount),
			vertexOffset = reference.vertexStart + group.vertexStart;

		// Apply offset to group indices
		for (let i = 0; i < groupIndices.length; i++) groupIndices[i] += vertexOffset;

		// Replace buffer data
		gl.bufferSubData(gl.ELEMENT_ARRAY_BUFFER, indexStart * 2, groupIndices);

		this.meshGroups.add(group); // Just a flag
		return true;
	}

	/**
	 * Drawing wires ain't going to be so simple. Can't use actual mesh VAO as
	 * it uses own index buffer for line drawing, but it does want positions
	 * from VMeshData.
	 *
	 * 1) Generate separate positions buffer in map (A) VMeshData->WebGLBuffer with
	 * only position attribute.
	 *
	 * 2) Generate indices (elements) buffer in map (B) VWireData->WebGLBuffer with
	 * indices pre-aligned to match those in position buffer.
	 *
	 * 3) Get position buffer from map (A) association to VMeshData from
	 * part.wireframe.meshID.
	 * 4) Get elements buffer by from map (B) by VWireData itself.
	 * 5) Draw!
	 */

	/**
	 * Draw VMeshGroup
	 * @param  {VMeshGroup} group      Mesh group
	 * @param  {Number}     indexStart Vertex array start index
	 * @return {Boolean}
	 */
	drawVMeshGroup(group, indexStart, highlight) {
		const gl = this.context;

		let material   = Glancer.resources.getMaterial(group.materialID),
			program    = this.getProgram(material.shader),
			renderMode = 0,
			indexCount = group.indexCount;

		// if (material.alpha) return false;

		gl.useProgram(program);

		let ambient  = this.ambientColor,
			texture0 = gl.getUniformLocation(program, "uTexture0"),
			texture1 = gl.getUniformLocation(program, "uTexture1");

		//  || ! (this.mode & DRAW_LIGHTING)
		if (this.status == status.BACKDROP) ambient = WHITE;
		if (highlight) ambient = WHITE;

		// Set blending mode
		if (material instanceof NebulaMaterial) {
			gl.blendFunc(gl.SRC_ALPHA, gl.ONE);
			renderMode = 10;
		} else {
			gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
		}

		// Load diffuse texture
		if (material.diffuseTexture && this.mode) {
			let diffuseTexture = Glancer.resources.getTexture(material.diffuseTexture);
			diffuseTexture = this.getTexture(diffuseTexture, material.alpha);

			gl.activeTexture(gl.TEXTURE0);
			gl.bindTexture(gl.TEXTURE_2D, diffuseTexture);
			gl.uniform1i(texture0, 0);
			renderMode |= mapModes.MAP_DIFFUSE;
		}

		if (material.emissionTexture && this.mode) {
			let emissionTexture = Glancer.resources.getTexture(material.emissionTexture);
			emissionTexture = this.getTexture(emissionTexture, false);

			gl.activeTexture(gl.TEXTURE1);
			gl.bindTexture(gl.TEXTURE_2D, emissionTexture);
			gl.uniform1i(texture1, 1);

			renderMode |= mapModes.MAP_EMISSION;
		}

		// Setup uniforms
		/*
		let mvp          = gl.getUniformLocation(program, "uModelViewProjection"),
			modelView    = gl.getUniformLocation(program, "uModelView"),
			model        = gl.getUniformLocation(program, "uModel"),
			normal       = gl.getUniformLocation(program, "uNormal");

		gl.uniformMatrix4fv(mvp,       false, this.matrices.result);
		gl.uniformMatrix4fv(modelView, false, this.matrices.modelView);
		gl.uniformMatrix4fv(model,     false, this.matrices.model);
		gl.uniformMatrix3fv(normal,    false, this.matrices.normal);
		*/

		let model      = gl.getUniformLocation(program, "uModel"),
			view       = gl.getUniformLocation(program, "uView"),
			projection = gl.getUniformLocation(program, "uProjection"),
			normal     = gl.getUniformLocation(program, "uNormal");

		gl.uniformMatrix4fv(model,      false, this.matrices.model);
		gl.uniformMatrix4fv(view,       false, this.matrices.view);
		gl.uniformMatrix4fv(projection, false, this.matrices.projection);
		gl.uniformMatrix3fv(normal,     false, this.matrices.normal);

		let mapsMode     = gl.getUniformLocation(program, "uTextureMode"),
			diffuseColor = gl.getUniformLocation(program, "uDiffuseColor");

		let ambientColor  = gl.getUniformLocation(program, "uAmbientColor"),
			lightCount    = gl.getUniformLocation(program, "uLightCount"),
			lightPosition = gl.getUniformLocation(program, "uLightPosition"),
			lightColor    = gl.getUniformLocation(program, "uLightColor");

		gl.uniform1i(mapsMode, this.mode & DRAW_TEXTURES ? renderMode : 0);
		gl.uniform4f(diffuseColor, material.diffuseColor[0], material.diffuseColor[1], material.diffuseColor[2], material.opacity);
		gl.uniform3fv(ambientColor, ambient);

		// Lights
		if (this.status == status.OBJECTS) {
			gl.uniform1i(lightCount,     this.mode & DRAW_LIGHTING ? this.lightCount : 0);
			gl.uniform3fv(lightPosition, this.lightPositions);
			gl.uniform3fv(lightColor,    this.lightColors);
		}

		// Draw mesh group triangles
		gl.drawElements(gl.TRIANGLES, indexCount, gl.UNSIGNED_SHORT, indexStart * 2);
		this.drawCount++;
		return true;
	}
}

/**
 * @property {Float32Array} result     Resulting Model-View-Projection matrix
 * @property {Float32Array} model      Target object final transformation
 * @property {Float32Array} view       Camera transformation
 * @property {Float32Array} projection Perspective/orthogonal transformation
 * @property {Float32Array} normal     Inverse transpose model matrix for normals and lighting
 */
class RenderMatrices {
	constructor() {
		let index = 0;

		this.result      = new Float32Array(16);
		this.model       = new Float32Array(16);
		this.view        = new Float32Array(16);
		this.projection  = new Float32Array(16);
		this.modelView   = new Float32Array(16);
		this.normal      = new Float32Array(9);
		this.boundary    = vec3.create();
		this.boundary2   = vec3.create();
		this.boundaryRadius = vec3.create();
	}

	/**
	 * Set projection to perspective matrix
	 * @param  {Number} fov         Vertical field of vision (radians)
	 * @param  {Number} aspectRatio Should be viewport aspect ratio
	 * @param  {Number} near        Near plane distance
	 * @param  {Number} far         Far plane distance
	 * @return {Boolean}
	 */
	perspective(fov, aspectRatio, near, far) {
		mat4.perspective(this.projection, fov, aspectRatio, near, far);
		return true;
	}

	build() {
		mat4.multiply(this.result, this.projection, this.view);
		mat4.multiply(this.result, this.result, this.model);
		mat4.multiply(this.modelView, this.view, this.model);
		mat3.normalFromMat4(this.normal, this.modelView);
	}
}

/**
 * Matrix stack of fixed size
 *
 * 
 */
class MatrixStack {
	constructor(count = 1) {
		this.buffer = new ArrayBuffer(Float32Array.BYTES_PER_ELEMENT * 16 * count);
	}

	/**
	 * Multiplies all matrices in stack to receiving matrix
	 * @param  {mat4} out
	 * @return {mat4}
	 */
	multiply(out) {

	}
}