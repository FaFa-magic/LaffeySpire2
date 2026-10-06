shader_type canvas_item;
render_mode unshaded;

uniform sampler2D sleep_tex : source_color, filter_linear, repeat_disable;
uniform sampler2D dream_tex : source_color, filter_linear, repeat_disable;
uniform float threshold : hint_range(0.0, 1.0) = 0.0;

const float DURATION = 4.5;
const float IMAGE_ASPECT = 1920.0 / 1080.0;
const vec2 FACE = vec2(0.515, 0.575);

float noise21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec2 fit_screen(vec2 screen, float aspect) {
	vec2 p = screen - vec2(0.5);
	if (aspect > IMAGE_ASPECT) {
		p.y *= IMAGE_ASPECT / aspect;
	} else {
		p.x *= aspect / IMAGE_ASPECT;
	}
	return p;
}

vec3 sleep_sample(vec2 p, float blur) {
	vec2 delta = vec2(1.0 / 1920.0, 1.0 / 1080.0) * blur;
	vec3 col = texture(sleep_tex, p).rgb * 0.40;
	col += texture(sleep_tex, p + delta * vec2(1.0, 1.0)).rgb * 0.15;
	col += texture(sleep_tex, p + delta * vec2(-1.0, 1.0)).rgb * 0.15;
	col += texture(sleep_tex, p + delta * vec2(1.0, -1.0)).rgb * 0.15;
	col += texture(sleep_tex, p - delta).rgb * 0.15;
	return col;
}

float sleep_bubbles(vec2 image_uv, float seconds) {
	float result = 0.0;
	for (int i = 0; i < 3; i++) {
		float seed = float(i);
		float cycle = fract(seconds / 2.4 + seed / 3.0);
		vec2 center = vec2(0.635, 0.45);
		center += vec2(0.02 * cycle + sin(cycle * 4.0 + seed) * 0.005, -0.155 * cycle);
		float radius = mix(0.006, 0.015, cycle);
		float distance = length((image_uv - center) * vec2(IMAGE_ASPECT, 1.0));
		float ring = 1.0 - smoothstep(0.0009, 0.0023, abs(distance - radius));
		float gleam = 1.0 - smoothstep(radius * 0.12, radius * 0.27,
			length((image_uv - center) * vec2(IMAGE_ASPECT, 1.0) - vec2(-radius * 0.36, -radius * 0.36)));
		float life = smoothstep(0.0, 0.15, cycle) * (1.0 - smoothstep(0.72, 1.0, cycle));
		result = max(result, max(ring, gleam) * life);
	}
	return result;
}

float dream_stars(vec2 screen, float seconds, float aspect) {
	vec2 p = screen * vec2(aspect, 1.0);
	float result = 0.0;
	for (int i = 0; i < 10; i++) {
		float seed = float(i);
		vec2 center = vec2(noise21(vec2(seed, 8.2)) * aspect, noise21(vec2(seed, 19.3)));
		center.y -= (seconds - 2.2) * 0.017;
		center.x += sin(seconds * 0.7 + seed * 2.0) * 0.01;
		vec2 local = abs(p - center);
		float radius = 0.004 + 0.004 * noise21(vec2(seed, 4.6));
		float diamond = local.x + local.y;
		float glow = exp(-length(local) * 100.0) * 0.17;
		float twinkle = 0.55 + 0.45 * sin(seconds * 2.5 + seed * 1.8);
		result += ((1.0 - smoothstep(radius * 0.6, radius, diamond)) + glow) * twinkle;
	}
	return clamp(result, 0.0, 1.0);
}

void fragment() {
	float progress = clamp(threshold, 0.0, 1.0);
	float seconds = progress * DURATION;
	float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
	vec2 fitted = fit_screen(SCREEN_UV, aspect);
	float push = smoothstep(1.05, 2.95, seconds);
	float zoom = mix(1.0, 2.65, push);
	vec2 center = mix(vec2(0.5), FACE, push);
	vec2 image_uv = fitted / zoom + center;
	float breathing = sin(seconds * 2.9) * 0.0045 * (1.0 - smoothstep(2.55, 3.3, seconds));
	float body_mask = 1.0 - smoothstep(0.7, 1.0, length((image_uv - vec2(0.52, 0.61)) / vec2(0.29, 0.36)));
	image_uv = mix(image_uv, (image_uv - FACE) / (1.0 + breathing) + FACE + vec2(0.0, breathing * 0.35), body_mask);
	float dream_phase = smoothstep(2.35, 3.5, seconds);
	vec3 scene = sleep_sample(image_uv, dream_phase * 5.0);
	float bubble = sleep_bubbles(image_uv, seconds) * smoothstep(0.3, 0.65, seconds)
		* (1.0 - smoothstep(2.5, 3.1, seconds));
	scene = mix(scene, vec3(1.0, 0.95, 0.98), bubble * 0.85);
	float distance = length((SCREEN_UV - vec2(0.5, 0.49)) * vec2(aspect, 1.0));
	float radius = mix(-0.18, max(aspect, 1.0), dream_phase);
	float dream_reveal = (1.0 - smoothstep(radius - 0.17, radius + 0.17, distance)) * dream_phase;
	dream_reveal = mix(dream_reveal, 1.0, smoothstep(0.88, 1.0, dream_phase));
	vec2 dream_uv = fitted / mix(1.0, 1.3, smoothstep(2.45, 4.1, seconds)) + vec2(0.5, 0.5);
	float cloud_mask = smoothstep(0.43, 0.9, dream_uv.y);
	dream_uv += vec2(sin(seconds * 0.8 + dream_uv.y * 8.0) * 0.003,
		cos(seconds * 0.7 + dream_uv.x * 6.0) * 0.002) * cloud_mask;
	vec3 dream = texture(dream_tex, dream_uv).rgb;
	dream = mix(dream, vec3(1.0, 0.94, 0.79), dream_stars(SCREEN_UV, seconds, aspect) * 0.75);
	float halo = exp(-abs(distance - max(radius, 0.0)) * 18.0) * sin(dream_phase * PI) * 0.22;
	scene = mix(scene, dream, dream_reveal);
	scene = mix(scene, vec3(0.98, 0.88, 0.97), halo);
	float vignette = smoothstep(0.25, 1.1, length((SCREEN_UV - vec2(0.5)) * vec2(aspect * 0.7, 1.0)));
	scene *= 1.0 - vignette * 0.10;
	float appear = smoothstep(0.0, 0.45, seconds);
	float fade = smoothstep(3.8, DURATION, seconds);
	COLOR = vec4(scene * (1.0 - fade), appear);
}
