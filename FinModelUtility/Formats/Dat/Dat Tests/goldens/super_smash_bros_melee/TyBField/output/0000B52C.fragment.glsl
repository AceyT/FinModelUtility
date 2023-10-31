# version 400

in vec4 vertexColor0;

out vec4 fragColor;

void main() {
  vec3 colorComponent = vec3(2)*vertexColor0.rgb;

  float alphaComponent = 0.5*vertexColor0.a;

  fragColor = vec4(colorComponent, alphaComponent);

  if (!(fragColor.a >= 0.95)) {
    discard;
  }
}
