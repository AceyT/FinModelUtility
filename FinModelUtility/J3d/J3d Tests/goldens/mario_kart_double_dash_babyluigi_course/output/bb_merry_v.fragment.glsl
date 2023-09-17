# version 400

uniform sampler2D texture0;
uniform vec3 color_GxMaterialColor0;
uniform float scalar_GxMaterialAlpha0;

in vec4 vertexColor0;
in vec4 vertexColor1;
in vec2 uv0;

out vec4 fragColor;

void main() {
  vec3 colorComponent = clamp(texture(texture0, uv0).rgb*color_GxMaterialColor0, 0, 1);

  float alphaComponent = texture(texture0, uv0).a*scalar_GxMaterialAlpha0;

  fragColor = vec4(colorComponent, alphaComponent);

  if (!(fragColor.a >= 0.5019608)) {
    discard;
  }
}
