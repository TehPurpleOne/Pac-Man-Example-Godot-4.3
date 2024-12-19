shader_type canvas_item;

uniform sampler2D palette;              // Use palettes in column with colors in rows
uniform float palette_count = 1.0;      // Used for highlighting hovered color
uniform float palette_index = 0.0;      // Set for choosing different palettes (0 is first)

void fragment() {
    float increment = 1.0 / palette_count;          // Value for getting palette index
    float y = increment * palette_index + increment * 0.5;  // + safety measure for floating point imprecision
    vec4 color = texture(TEXTURE, UV);              // Original grayscale color used as column index
    vec4 new_color = texture(palette, vec2(color.r, y));  // Get color from palette texture
    float a = step(0.00392, color.a);               // Check if transparent color is less than 1/255 for backgrounds
    new_color.a *= a;                               // If BG is transparent, alpha is multiplied by 0

    COLOR = new_color;                              // Set new color from palette
}