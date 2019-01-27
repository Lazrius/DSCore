/**
 * Materials in Freelancer are fairly simple, utilizing at most two textures at
 * once, a limitation probably dictated by target minimum hardware at the time. 
 */
import {ArrayBufferWalker} from "./core.js";
import {flcrc32} from "./hash.js";
import {UTFEntry} from "./utf.js";

export class Material {
	constructor() {
		this.shader = "standard-vertex";
		this.diffuseColor = new Float32Array(3);
		this.diffuseColor.fill(1);
		this.opacity = 1;
	}

	load(folder) {

	}
}

export class SinglePassMaterial extends Material {
	constructor() {
		super();
	}

	load(folder) {
		let [diffuseColor, diffuseTexture, diffuseFlags, opacityValue, emissionColor, emissionTexture, emissionFlags] = folder.find("dc", "dt_name", "dt_flags", "oc", "ec", "et_name", "et_flags");

		// Diffuse color tint
		if (this.type.includes("Dc") && diffuseColor)
			this.diffuseColor = diffuseColor.data.readFloat32(3);

		// Diffuse texture
		if (this.type.includes("Dt") && diffuseTexture) {
			this.diffuseTexture = flcrc32(diffuseTexture.data.readString());
			if (diffuseFlags) [this.diffuseFlags] = diffuseFlags.data.readInt32(1);
		}

		// Opacity value (there's no opacity texture, alpha is provided by diffuse texture)
		if (this.type.includes("Oc") && opacityValue) {
			this.alpha = true;
			if (opacityValue) [this.opacity] = opacityValue.data.readFloat32(1);
		}

		// Emission color
		if (this.type.includes("Ec") && emissionColor)
			this.emissionColor = emissionColor.data.readFloat32(3);

		// Emission texture
		if (this.type.includes("Et") && emissionTexture) {
			this.emissionTexture = emissionTexture.data.readString();
			if (emissionFlags) [this.emissionFlags] = emissionFlags.data.readInt32(1);
		}
	}
}

export class DetailMaterial extends Material {
	load(folder) {
		let [detailTexture, detailFlags] = folder.find("bt_name", "bt_flags");

		this.detailTexture = detailTexture.data.readString();
		if (detailFlags) [this.detailFlags] = detailFlags.data.readInt32(1);
	}
}

export class NebulaMaterial extends Material {

	load(folder) {
		let [diffuseColor, diffuseTexture, diffuseFlags, opacityValue, emissionColor] = folder.find("dc", "dt_name", "dt_flags", "oc", "ec");

		// Diffuse color tint
		if (diffuseColor)
			this.diffuseColor = diffuseColor.data.readFloat32(3);

		// Diffuse texture
		if (diffuseTexture) {
			this.diffuseTexture = flcrc32(diffuseTexture.data.readString());
			if (diffuseFlags) [this.diffuseFlags] = diffuseFlags.data.readInt32(1);
		}

		// Opacity value (there's no opacity texture, alpha is provided by diffuse texture)
		if (opacityValue) {
			this.alpha = true;
			if (opacityValue) [this.opacity] = opacityValue.data.readFloat32(1);
		}

		// Emission color
		if (emissionColor)
			this.emissionColor = emissionColor.data.readFloat32(3);
	}
}

export class NomadMaterial extends Material {
	load(folder) {
		let [nomadTexture, nomadFlags] = folder.find("nt_name", "nt_flags");

		this.nomadTexture = nomadTexture.data.readString();
		if (nomadFlags) [this.nomadFlags] = nomadFlags.data.readInt32(1);
	}
}

export class GlassMaterial extends Material {
	constructor() {
		super();

		this.alpha = true;
		this.shader = "glass";
	}

	load(folder) {
		let [diffuseColor, opacityValue] = folder.find("dc", "oc");

		// Diffuse color tint
		if (diffuseColor) this.diffuseColor = diffuseColor.data.readFloat32(3);

		// Opacity value (there's no opacity texture, alpha is provided by diffuse texture)
		if (opacityValue) [this.opacity] = opacityValue.data.readFloat32(1);
	}
}


/**
 * Material name patterns determining special material types
 *
 * Freelancer has some material type overrides based on matching material name
 * to a pattern Patterns and material types are listed in EXE\dacom.ini
 *
 * alpha_mask materials don't actually use alpha for blending but for masking
 */
const patterns = new Map();

patterns.set(/^alpha_mask.*/,      SinglePassMaterial);  // DcDt
patterns.set(/^alpha_mask.*2side/, SinglePassMaterial);  // DcDtTwo
patterns.set(/^detailmap_.*/,      DetailMaterial);      // BtDetailMapMaterial
patterns.set(/^tlr_material$/,     NebulaMaterial);      // NebulaTwo
patterns.set(/^tlr_energy$/,       NebulaMaterial);      // NebulaTwo
patterns.set(/^nomad.*$/,          NomadMaterial);       // NomadMaterialNoBendy
patterns.set(/^n-texture.*$/,      NomadMaterial);       // NomadMaterialNoBendy
// patterns.set(/^ui_.*/,             IconMaterial);        // HUDIconMaterial
patterns.set(/^exclusion_.*/,      SinglePassMaterial);  // ExclusionZoneMaterial
patterns.set(/^c_glass$/,          GlassMaterial);       // HighGlassMaterial
patterns.set(/^cv_glass$/,         GlassMaterial);       // HighGlassMaterial
patterns.set(/^b_glass$/,          GlassMaterial);       // HighGlassMaterial
patterns.set(/^k_glass$/,          GlassMaterial);       // HighGlassMaterial
patterns.set(/^l_glass$/,          GlassMaterial);       // HighGlassMaterial
patterns.set(/^r_glass$/,          GlassMaterial);       // HighGlassMaterial
patterns.set(/^planet.*_glass$/,   GlassMaterial);       // GFGlassMaterial
patterns.set(/^bw_glass$/,         GlassMaterial);       // HighGlassMaterial
patterns.set(/^o_glass$/,          GlassMaterial);       // HighGlassMaterial
// patterns.set(/^anim_hud.*$/,       HUDMaterial);         // HUDAnimMaterial
// patterns.set(/^sea_anim.*$/,       PlanetWaterMaterial); // PlanetWaterMaterial
// patterns.set(/^null$/,             NullMaterial);        // NullMaterial

/**
 * Material type replacements
 */
const replacements = new Map();
replacements.set("EcEtOcOt", "DcDtOcOt");
replacements.set("DcDtEcEt", "DcDtEt");

export function loadMaterial(folder) {
	if (! (folder instanceof UTFEntry)) throw new TypeError("Invalid material entry object");
	if (! folder.hasChild) return false;

	let twoSided = false, alpha = false, material, materialClass, entry, type;

	// Name pattern overrides lookup
	for (const [pattern, overrideClass] of patterns)
		if (! materialClass && pattern.test(folder.tag)) materialClass = overrideClass;
	
	// Determine material class from type
	if (! materialClass) {
		[entry] = folder.find("type");
		if (! entry) return false;

		type = entry.data.readString();

		// Type replacement lookup
		if (replacements.has(type)) type = replacements.get(type);

		switch (type) {
			case "DcDtTwo":
			case "DcDtEcTwo":
			case "DcDtOcOtTwo":
			case "DcDtEcOcOtTwo": twoSided = true;
			case "EcEt":
			case "DcDt":
			case "DcDtEc":
			case "DcDtEt":
			case "DcDtEcEt":
			case "DcDtOcOt":
			case "DcDtEcOcOt":    materialClass = SinglePassMaterial; break;
			case "BtDetailMapMaterial": materialClass = DetailMaterial; break;
			case "NebulaTwo":     twoSided = true;
			case "Nebula":        materialClass = NebulaMaterial; alpha = true; break;
			case "NomadMaterial": materialClass = NomadMaterial; alpha = true; break;
			// TODO: Lots of other special types should go there
			case "NullMaterial":
			default:              materialClass = Material;
		}
	}

	if (! materialClass) return false;

	// Create material instance and load its properties
	material      = new materialClass();

	if (type)     material.type     = type;
	if (twoSided) material.twoSided = true;
	if (alpha)    material.alpha    = true;

	material.load(folder);
	return material;
}