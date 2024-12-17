shader_type canvas_item;

uniform sampler2D palette;
uniform int index = 0;

void fragment() {
	ivec2 palette_size = textureSize(palette, 0);
	float increment = 1.0/float(palette_size.y);
	float y = increment * float(index % palette_size.y);
	vec4 grey_index = texture(TEXTURE, UV);
	vec4 new_color = texture(palette, vec2(grey_index.r, y));
	new_color.a *= grey_index.a;
	COLOR = new_color;
}