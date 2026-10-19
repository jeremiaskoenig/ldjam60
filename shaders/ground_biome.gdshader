shader_type spatial;

uniform sampler2D biome_map : filter_linear, repeat_disable;
uniform float map_size = 4000.0;
uniform sampler2D border_noise : hint_default_white, filter_linear, repeat_enable;
uniform float border_noise_strength = 40.0;

uniform sampler2D variation_tex : hint_default_white, filter_linear, repeat_enable;
uniform float variation_size = 250.0;
uniform float variation_min = 0.8;
uniform float variation_max = 1.1;

group_uniforms desert;
uniform sampler2D desert_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D desert_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D desert_roughness : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float desert_tile = 4.0;

group_uniforms city;
uniform sampler2D city_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D city_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D city_roughness : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float city_tile = 3.0;

group_uniforms forest;
uniform sampler2D forest_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_roughness : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float forest_tile = 3.0;
group_uniforms;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    TANGENT = vec3(1.0, 0.0, 0.0);
    BINORMAL = vec3(0.0, 0.0, 1.0);
}

void fragment() {
    vec2 noise_uv = world_pos.xz / 300.0;
    vec2 jitter = vec2(
        texture(border_noise, noise_uv).r,
        texture(border_noise, noise_uv + vec2(0.37, 0.71)).r
    ) * 2.0 - 1.0;

    vec2 biome_uv = (world_pos.xz + jitter * border_noise_strength) / map_size;
    vec3 w = texture(biome_map, biome_uv).rgb;
    w /= max(w.r + w.g + w.b, 0.0001);

    vec2 uv_d = world_pos.xz / desert_tile;
    vec2 uv_c = world_pos.xz / city_tile;
    vec2 uv_f = world_pos.xz / forest_tile;

    vec3 albedo = texture(desert_albedo, uv_d).rgb * w.r
                + texture(city_albedo, uv_c).rgb * w.g
                + texture(forest_albedo, uv_f).rgb * w.b;

    vec3 normal = texture(desert_normal, uv_d).rgb * w.r
                + texture(city_normal, uv_c).rgb * w.g
                + texture(forest_normal, uv_f).rgb * w.b;

    float rough = texture(desert_roughness, uv_d).r * w.r
                + texture(city_roughness, uv_c).r * w.g
                + texture(forest_roughness, uv_f).r * w.b;

    float variation = texture(variation_tex, world_pos.xz / variation_size).r;

    ALBEDO = albedo * mix(variation_min, variation_max, variation);
    NORMAL_MAP = normal;
    ROUGHNESS = rough;
}