import * as THREE from 'three';

const TARGET_HEIGHT = 1.68;
// Distance travelled by the current model over one complete left/right stride.
const STRIDE_METERS = 1.12 * 32 / 30;

export class TargetActor {
  constructor(scene, route, onAssetStatus) {
    this.route = route.map(([x, z]) => new THREE.Vector3(x, 0, z));
    this.routeIndex = 1;
    this.speed = 1.12;
    this.finished = false;
    this.mixer = null;
    this.walkAction = null;
    this.idleAction = null;
    this.currentAction = null;
    this.ready = false;
    this.onAssetStatus = onAssetStatus;
    this.root = new THREE.Group();
    scene.add(this.root);
    this.reset();
    this.onAssetStatus('V4 캐릭터 불러오는 중');
    this.loadModel();
  }

  async loadModel() {
    try {
      const { GLTFLoader } = await import('three/addons/loaders/GLTFLoader.js');
      const gltf = await new GLTFLoader().loadAsync(`${import.meta.env.BASE_URL}models/woman-v4-walk.glb`);
      const model = gltf.scene;
      model.name = 'Woman V4';
      const walk = THREE.AnimationClip.findByName(gltf.animations, 'Walk');
      const idle = THREE.AnimationClip.findByName(gltf.animations, 'Idle');
      if (!walk || !idle) throw new Error('Walking animation is missing');

      model.traverse((object) => {
        object.frustumCulled = true;
        if (object.isMesh) {
          object.castShadow = false;
          object.receiveShadow = false;
        }
      });
      const bounds = new THREE.Box3().setFromObject(model);
      const height = Math.max(bounds.max.y - bounds.min.y, 0.01);
      model.scale.setScalar(TARGET_HEIGHT / height);
      model.updateMatrixWorld(true);
      model.position.y -= new THREE.Box3().setFromObject(model).min.y;
      this.root.add(model);
      this.mixer = new THREE.AnimationMixer(model);
      this.walkAction = this.mixer.clipAction(walk);
      this.idleAction = this.mixer.clipAction(idle);
      this.currentAction = this.idleAction.play();
      this.mixer.update(0);
      this.ready = true;
      this.onAssetStatus('V4 캐릭터 준비 완료', true);
    } catch (error) {
      console.error('V4 캐릭터 로드 실패:', error);
      this.onAssetStatus('V4 캐릭터를 불러오지 못했습니다');
    }
  }

  reset() {
    if (this.mixer) {
      this.mixer.stopAllAction();
      this.currentAction = this.idleAction.reset().play();
      this.mixer.update(0);
    }
    this.routeIndex = 1;
    this.finished = false;
    this.root.position.copy(this.route[0]);
    this.faceNextWaypoint();
  }

  faceNextWaypoint() {
    const next = this.route[Math.min(this.routeIndex, this.route.length - 1)];
    const direction = next.clone().sub(this.root.position);
    if (direction.lengthSq() > 0.001) this.root.rotation.y = Math.atan2(direction.x, direction.z);
  }

  update(delta, active = true) {
    if (!active || this.finished) {
      this.animateWalk(false, delta);
      return;
    }
    let waypoint = this.route[this.routeIndex];
    let deltaToTarget = waypoint.clone().sub(this.root.position).setY(0);
    if (deltaToTarget.length() < 0.18) {
      if (this.routeIndex >= this.route.length - 1) {
        this.finished = true;
        this.animateWalk(false, delta);
        return;
      }
      this.routeIndex += 1;
      waypoint = this.route[this.routeIndex];
      deltaToTarget = waypoint.clone().sub(this.root.position).setY(0);
    }
    const direction = deltaToTarget.normalize();
    const targetYaw = Math.atan2(direction.x, direction.z);
    this.root.rotation.y = rotateTowards(this.root.rotation.y, targetYaw, delta * 2.6);
    this.root.position.addScaledVector(direction, this.speed * delta);
    this.animateWalk(true, delta);
  }

  animateWalk(walking, delta) {
    if (!this.mixer) return;
    const next = walking ? this.walkAction : this.idleAction;
    if (next !== this.currentAction) {
      next.reset().play().crossFadeFrom(this.currentAction, 0.15, false);
      this.currentAction = next;
    }
    this.walkAction.timeScale = this.speed * this.walkAction.getClip().duration / STRIDE_METERS;
    this.mixer.update(delta);
  }
}

function rotateTowards(current, target, maxDelta) {
  let difference = ((target - current + Math.PI) % (Math.PI * 2)) - Math.PI;
  if (difference < -Math.PI) difference += Math.PI * 2;
  return current + THREE.MathUtils.clamp(difference, -maxDelta, maxDelta);
}
