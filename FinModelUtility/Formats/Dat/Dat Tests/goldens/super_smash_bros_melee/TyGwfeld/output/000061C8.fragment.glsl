#version 400


out vec4 fragColor;

void main() {
  vec3 colorComponent = vec3(0);

  float alphaComponent = 0.20000000298023224;

  fragColor = vec4(colorComponent, alphaComponent);

  if (!(fragColor.a > 0)) {
    discard;
  }
}
