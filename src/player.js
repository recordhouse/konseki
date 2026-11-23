import * as THREE from 'three';

const PLAYER_RADIUS = 0.34;
const EYE_HEIGHT = 1.65;

export class FirstPersonPlayer {
  constructor(camera, canvas, colliders) {
    this.camera = camera;
    this.canvas = canvas;
    this.colliders = colliders;
    this.position = new THREE.Vector3(0, EYE_HEIGHT, 2);
    this.velocity = new THREE.Vector3();
    this.keys = new Set();
    this.yaw = Math.PI;
    this.pitch = 0;
    this.walkTime = 0;
    this.enabled = false;

    document.addEventListener('mousemove', (event) => this.onMouseMove(event));
    window.addEventListener('keydown', (event) => this.keys.add(event.code));
    window.addEventListener('keyup', (event) => this.keys.delete(event.code));
    this.syncCamera();
  }

  setEnabled(enabled) {
    this.enabled = enabled;
    if (!enabled) this.keys.clear();
  }

  reset() {
    this.position.set(0, EYE_HEIGHT, 2);
    this.velocity.set(0, 0, 0);
    this.yaw = Math.PI;
    this.pitch = 0;
    this.walkTime = 0;
    this.syncCamera();
  }

  onMouseMove(event) {
    if (!this.enabled || document.pointerLockElement !== this.canvas) return;
    const sensitivity = 0.00235;
    this.yaw -= event.movementX * sensitivity;
    this.pitch -= event.movementY * sensitivity;
    this.pitch = THREE.MathUtils.clamp(this.pitch, -1.42, 1.42);
  }

  update(delta) {
    if (!this.enabled) {
      this.syncCamera();
      return;
    }

    const input = new THREE.Vector2(
      Number(this.keys.has('KeyD')) - Number(this.keys.has('KeyA')),
      Number(this.keys.has('KeyW')) - Number(this.keys.has('KeyS')),
    );
    if (input.lengthSq() > 1) input.normalize();

    const forward = new THREE.Vector3(-Math.sin(this.yaw), 0, -Math.cos(this.yaw));
    const right = new THREE.Vector3(-forward.z, 0, forward.x);
    const desired = forward.multiplyScalar(input.y).add(right.multiplyScalar(input.x)).multiplyScalar(2.35);
    const response = 1 - Math.exp(-delta * 12);
    this.velocity.x = THREE.MathUtils.lerp(this.velocity.x, desired.x, response);
    this.velocity.z = THREE.MathUtils.lerp(this.velocity.z, desired.z, response);

    const proposed = this.position.clone().addScaledVector(this.velocity, delta);
    proposed.x = THREE.MathUtils.clamp(proposed.x, -30, 30);
    proposed.z = THREE.MathUtils.clamp(proposed.z, -2, 94);
    if (!this.collides(proposed.x, this.position.z)) this.position.x = proposed.x;
    if (!this.collides(this.position.x, proposed.z)) this.position.z = proposed.z;

    const moving = input.lengthSq() > 0.01;
    if (moving) this.walkTime += delta * 8.2;
    this.position.y = EYE_HEIGHT + (moving ? Math.sin(this.walkTime) * 0.018 : 0);
    this.syncCamera();
  }

  collides(x, z) {
    for (const box of this.colliders) {
      if (
        x + PLAYER_RADIUS > box.min.x && x - PLAYER_RADIUS < box.max.x &&
        z + PLAYER_RADIUS > box.min.z && z - PLAYER_RADIUS < box.max.z
      ) return true;
    }
    return false;
  }

  syncCamera() {
    this.camera.position.copy(this.position);
    this.camera.rotation.order = 'YXZ';
    this.camera.rotation.set(this.pitch, this.yaw, 0);
  }
}
