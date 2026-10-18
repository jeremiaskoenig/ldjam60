shader_type spatial;

uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D normal_tex : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roughness_tex : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D variation_tex : hint_default_white, filter_linear, repeat_enable;

uniform float tile_size = 4.0;
uniform float variation_size = 250.0;
uniform float variation_min = 0.75;
uniform float variation_max = 1.15;
uniform float normal_strength = 1.0;

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	TANGENT = vec3(1.0, 0.0, 0.0);
	BINORMAL = vec3(0.0, 0.0, 1.0);
}

void fragment() {
	vec2 uv = world_pos.xz / tile_size;
	float variation = texture(variation_tex, world_pos.xz / variation_size).r;

	ALBEDO = texture(albedo_tex, uv).rgb * mix(variation_min, variation_max, variation);
	NORMAL_MAP = texture(normal_tex, uv).rgb;
	NORMAL_MAP_DEPTH = normal_strength;
	ROUGHNESS = texture(roughness_tex, uv).r;
}
