import * as THREE from 'three';

const TARGET_HEIGHT = 1.68;

function material(color) {
  return new THREE.MeshToonMaterial({ color });
}

function mesh(geometry, mat, parent, position) {
  const value = new THREE.Mesh(geometry, mat);
  value.position.copy(position);
  parent.add(value);
  return value;
}

function createFallbackTarget() {
  const root = new THREE.Group();
  root.name = 'Reference-based cartoon target woman';
  const skin = material(0xeab7a4);
  const skinShade = material(0xd89583);
  const hair = material(0x281d24);
  const hairLight = material(0x3a2931);
  const navy = material(0x18213b);
  const navyLight = material(0x2c395b);
  const shoe = material(0x11131a);
  const sole = material(0x07080c);
  const white = material(0xe8e8e5);
  const eye = material(0x251b23);
  const lip = material(0xb95f68);
  const body = new THREE.Group();
  root.add(body);

  // Curved adult silhouette beneath the uniform.
  const hips = mesh(new THREE.SphereGeometry(0.24, 18, 12), skinShade, body, new THREE.Vector3(0, 0.91, -0.005));
  hips.scale.set(1.32, 0.72, 0.9);
  const torso = mesh(new THREE.CapsuleGeometry(0.205, 0.29, 7, 14), white, body, new THREE.Vector3(0, 1.22, 0));
  torso.scale.set(1.18, 1, 0.82);
  const waist = mesh(new THREE.CylinderGeometry(0.205, 0.235, 0.16, 14), white, body, new THREE.Vector3(0, 1.02, 0));
  waist.scale.z = 0.82;

  // Navy sailor collar, bow and sleeve trim.
  const collarBack = mesh(new THREE.BoxGeometry(0.37, 0.17, 0.025), navy, body, new THREE.Vector3(0, 1.4, -0.176));
  collarBack.rotation.z = Math.PI * 0.25;
  collarBack.scale.y = 0.48;
  const collarBackRight = collarBack.clone();
  collarBackRight.rotation.z = -Math.PI * 0.25;
  body.add(collarBackRight);
  const collarFront = mesh(new THREE.TorusGeometry(0.16, 0.018, 6, 16, Math.PI), navy, body, new THREE.Vector3(0, 1.39, 0.16));
  collarFront.rotation.z = Math.PI;
  const bow = new THREE.Group();
  bow.position.set(0, 1.28, 0.195);
  body.add(bow);
  const bowLeft = mesh(new THREE.SphereGeometry(0.075, 10, 8), navy, bow, new THREE.Vector3(-0.055, 0, 0));
  bowLeft.scale.set(1.25, 0.58, 0.35);
  const bowRight = bowLeft.clone();
  bowRight.position.x = 0.055;
  bow.add(bowRight);
  mesh(new THREE.SphereGeometry(0.035, 9, 7), navyLight, bow, new THREE.Vector3(0, 0, 0.015));

  // Pleated skirt with alternating raised panels.
  const skirt = mesh(new THREE.CylinderGeometry(0.34, 0.255, 0.29, 14), navy, body, new THREE.Vector3(0, 0.89, 0));
  skirt.scale.z = 0.82;
  for (let index = 0; index < 12; index += 1) {
    const angle = (index / 12) * Math.PI * 2;
    const pleat = mesh(new THREE.BoxGeometry(0.035, 0.27, 0.018), navyLight, body, new THREE.Vector3(
      Math.sin(angle) * 0.288,
      0.88,
      Math.cos(angle) * 0.235,
    ));
    pleat.rotation.y = angle;
    pleat.rotation.z = Math.sin(angle) * 0.08;
  }

  // Head, face and layered long hair.
  const neck = mesh(new THREE.CylinderGeometry(0.067, 0.075, 0.12, 10), skin, body, new THREE.Vector3(0, 1.48, 0));
  neck.scale.z = 0.9;
  const headPivot = new THREE.Group();
  headPivot.position.set(0, 1.61, 0);
  body.add(headPivot);
  const face = mesh(new THREE.SphereGeometry(0.175, 20, 16), skin, headPivot, new THREE.Vector3(0, 0, 0));
  face.scale.set(0.9, 1.08, 0.88);
  const hairCap = mesh(new THREE.SphereGeometry(0.188, 20, 14, 0, Math.PI * 2, 0, 1.9), hair, headPivot, new THREE.Vector3(0, 0.035, -0.012));
  hairCap.scale.set(1, 1.04, 1.02);

  for (const side of [-1, 1]) {
    const eyeMesh = mesh(new THREE.SphereGeometry(0.028, 10, 7), eye, headPivot, new THREE.Vector3(side * 0.064, 0.015, 0.152));
    eyeMesh.scale.set(1.25, 0.52, 0.24);
    const highlight = mesh(new THREE.SphereGeometry(0.006, 7, 5), white, headPivot, new THREE.Vector3(side * 0.059, 0.022, 0.159));
    highlight.scale.set(0.8, 0.8, 0.35);
    const brow = mesh(new THREE.BoxGeometry(0.065, 0.009, 0.008), hair, headPivot, new THREE.Vector3(side * 0.063, 0.075, 0.148));
    brow.rotation.z = side * -0.12;
  }
  const mouth = mesh(new THREE.TorusGeometry(0.026, 0.006, 5, 10, Math.PI), lip, headPivot, new THREE.Vector3(0, -0.072, 0.157));
  mouth.rotation.z = Math.PI;
  mouth.scale.y = 0.55;

  const bangs = [];
  for (const [x, length, angle] of [[-0.09, 0.15, -0.2], [-0.035, 0.18, -0.08], [0.03, 0.17, 0.08], [0.09, 0.14, 0.22]]) {
    const bang = mesh(new THREE.CapsuleGeometry(0.035, length, 5, 8), hairLight, headPivot, new THREE.Vector3(x, 0.035, 0.135));
    bang.rotation.z = angle;
    bang.scale.z = 0.42;
    bangs.push(bang);
  }

  const hairLocks = [];
  const lockData = [
    [-0.16, 0.53, -0.13], [-0.11, 0.66, -0.16], [-0.055, 0.72, -0.17],
    [0, 0.75, -0.175], [0.055, 0.72, -0.17], [0.11, 0.66, -0.16], [0.16, 0.53, -0.13],
  ];
  for (let index = 0; index < lockData.length; index += 1) {
    const [x, length, z] = lockData[index];
    const lockPivot = new THREE.Group();
    lockPivot.position.set(x, 1.59, z);
    body.add(lockPivot);
    const lock = mesh(
      new THREE.CapsuleGeometry(index === 3 ? 0.075 : 0.062, length, 6, 9),
      index % 2 === 0 ? hairLight : hair,
      lockPivot,
      new THREE.Vector3(0, -length * 0.52, 0),
    );
    lock.scale.z = 0.58;
    lockPivot.rotation.z = (x / 0.16) * 0.08;
    hairLocks.push(lockPivot);
  }

  const limbs = {};
  for (const side of [-1, 1]) {
    const sideName = side < 0 ? 'left' : 'right';
    const armPivot = new THREE.Group();
    armPivot.position.set(side * 0.245, 1.36, 0);
    body.add(armPivot);
    const sleeve = mesh(new THREE.CapsuleGeometry(0.075, 0.12, 5, 9), white, armPivot, new THREE.Vector3(0, -0.09, 0));
    sleeve.scale.set(1.04, 1, 0.9);
    const sleeveTrim = mesh(new THREE.CylinderGeometry(0.077, 0.077, 0.026, 10), navy, armPivot, new THREE.Vector3(0, -0.19, 0));
    sleeveTrim.scale.z = 0.9;
    const elbowPivot = new THREE.Group();
    elbowPivot.position.set(0, -0.22, 0);
    armPivot.add(elbowPivot);
    mesh(new THREE.CapsuleGeometry(0.057, 0.25, 5, 9), skin, elbowPivot, new THREE.Vector3(0, -0.145, 0));
    const hand = mesh(new THREE.SphereGeometry(0.064, 10, 8), skin, elbowPivot, new THREE.Vector3(0, -0.32, 0.005));
    hand.scale.set(0.78, 1.12, 0.58);

    const legPivot = new THREE.Group();
    legPivot.position.set(side * 0.125, 0.82, 0);
    body.add(legPivot);
    const thigh = mesh(new THREE.CapsuleGeometry(0.11, 0.23, 6, 11), skin, legPivot, new THREE.Vector3(0, -0.19, 0));
    thigh.scale.z = 0.88;
    const kneePivot = new THREE.Group();
    kneePivot.position.set(0, -0.4, 0);
    legPivot.add(kneePivot);
    mesh(new THREE.CapsuleGeometry(0.087, 0.22, 6, 10), skin, kneePivot, new THREE.Vector3(0, -0.16, 0));
    const sock = mesh(new THREE.CylinderGeometry(0.084, 0.072, 0.29, 11), navy, kneePivot, new THREE.Vector3(0, -0.225, 0));
    sock.scale.z = 0.9;
    const shoeMesh = mesh(new THREE.BoxGeometry(0.17, 0.105, 0.31), shoe, kneePivot, new THREE.Vector3(0, -0.38, 0.072));
    shoeMesh.geometry.translate(0, 0, 0.03);
    const shoeSole = mesh(new THREE.BoxGeometry(0.18, 0.035, 0.33), sole, kneePivot, new THREE.Vector3(0, -0.435, 0.075));
    const strap = mesh(new THREE.BoxGeometry(0.13, 0.035, 0.12), navyLight, kneePivot, new THREE.Vector3(0, -0.33, 0.04));
    limbs[`${sideName}Arm`] = armPivot;
    limbs[`${sideName}Elbow`] = elbowPivot;
    limbs[`${sideName}Leg`] = legPivot;
    limbs[`${sideName}Knee`] = kneePivot;
  }

  root.userData.limbs = limbs;
  root.userData.body = body;
  root.userData.head = headPivot;
  root.userData.hairLocks = hairLocks;
  root.userData.skirt = skirt;
  root.userData.bangs = bangs;
  root.scale.setScalar(TARGET_HEIGHT / 1.78);
  return root;
}

export class TargetActor {
  constructor(scene, route, onAssetStatus) {
    this.route = route.map(([x, z]) => new THREE.Vector3(x, 0, z));
    this.routeIndex = 1;
    this.speed = 1.12;
    this.walkPhase = 0;
    this.finished = false;
    this.vrm = null;
    this.vrmBones = null;
    this.onAssetStatus = onAssetStatus;
    this.root = new THREE.Group();
    this.visual = createFallbackTarget();
    this.root.add(this.visual);
    scene.add(this.root);
    this.reset();
    this.tryLoadVrm();
  }

  async tryLoadVrm() {
    const [{ GLTFLoader }, { VRMLoaderPlugin }] = await Promise.all([
      import('three/addons/loaders/GLTFLoader.js'),
      import('@pixiv/three-vrm'),
    ]);
    const loader = new GLTFLoader();
    loader.register((parser) => new VRMLoaderPlugin(parser));
    try {
      const gltf = await loader.loadAsync('/models/Konseki.vrm');
      const vrm = gltf.userData.vrm;
      if (!vrm) throw new Error('VRM data missing');
      vrm.scene.traverse((object) => {
        object.frustumCulled = true;
        if (object.isMesh) {
          object.castShadow = false;
          object.receiveShadow = false;
        }
      });
      const bounds = new THREE.Box3().setFromObject(vrm.scene);
      const height = Math.max(bounds.max.y - bounds.min.y, 0.01);
      vrm.scene.scale.setScalar(TARGET_HEIGHT / height);
      vrm.scene.updateMatrixWorld(true);
      vrm.scene.position.y -= new THREE.Box3().setFromObject(vrm.scene).min.y;
      this.root.remove(this.visual);
      this.visual = vrm.scene;
      this.root.add(this.visual);
      this.vrm = vrm;
      this.cacheVrmBones();
      this.onAssetStatus('Konseki.vrm 로드 완료 · VRM 타겟 사용 중');
    } catch {
      this.onAssetStatus('참고 이미지 기반 카툰 3D 타겟 사용 중');
    }
  }

  cacheVrmBones() {
    const names = [
      'leftUpperArm', 'rightUpperArm', 'leftLowerArm', 'rightLowerArm',
      'leftUpperLeg', 'rightUpperLeg', 'leftLowerLeg', 'rightLowerLeg',
      'hips', 'chest', 'head',
    ];
    this.vrmBones = {};
    for (const name of names) {
      const bone = this.vrm?.humanoid?.getNormalizedBoneNode(name);
      if (bone) this.vrmBones[name] = { bone, rest: bone.quaternion.clone() };
    }
  }

  reset() {
    this.routeIndex = 1;
    this.finished = false;
    this.walkPhase = 0;
    this.root.position.copy(this.route[0]);
    this.faceNextWaypoint();
  }

  faceNextWaypoint() {
    const next = this.route[Math.min(this.routeIndex, this.route.length - 1)];
    const direction = next.clone().sub(this.root.position);
    if (direction.lengthSq() > 0.001) this.root.rotation.y = Math.atan2(direction.x, direction.z);
  }

  update(delta, active = true) {
    if (this.vrm) this.vrm.update(delta);
    if (!active || this.finished) {
      this.animateWalk(0);
      return;
    }
    let waypoint = this.route[this.routeIndex];
    let deltaToTarget = waypoint.clone().sub(this.root.position).setY(0);
    if (deltaToTarget.length() < 0.18) {
      if (this.routeIndex >= this.route.length - 1) {
        this.finished = true;
        this.animateWalk(0);
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
    this.walkPhase += delta * 6.1;
    this.animateWalk(1);
  }

  animateWalk(blend) {
    const stride = Math.sin(this.walkPhase) * blend;
    const bounce = Math.abs(Math.sin(this.walkPhase)) * 0.016 * blend;
    if (this.vrmBones) {
      this.rotateVrmBone('leftUpperLeg', stride * 0.34, 0, 0);
      this.rotateVrmBone('rightUpperLeg', -stride * 0.34, 0, 0);
      this.rotateVrmBone('leftLowerLeg', Math.max(0, -stride) * 0.38, 0, 0);
      this.rotateVrmBone('rightLowerLeg', Math.max(0, stride) * 0.38, 0, 0);
      this.rotateVrmBone('leftUpperArm', -stride * 0.22, 0, 1.15);
      this.rotateVrmBone('rightUpperArm', stride * 0.22, 0, -1.15);
      this.rotateVrmBone('chest', 0, 0, stride * 0.025);
      this.visual.position.y = bounce;
      return;
    }
    const limbs = this.visual.userData.limbs;
    if (!limbs) return;
    limbs.leftLeg.rotation.x = stride * 0.5;
    limbs.rightLeg.rotation.x = -stride * 0.5;
    limbs.leftKnee.rotation.x = Math.max(0, -stride) * 0.52;
    limbs.rightKnee.rotation.x = Math.max(0, stride) * 0.52;
    limbs.leftArm.rotation.x = -stride * 0.34;
    limbs.rightArm.rotation.x = stride * 0.34;
    limbs.leftElbow.rotation.x = -0.08 + Math.max(0, stride) * 0.12;
    limbs.rightElbow.rotation.x = -0.08 + Math.max(0, -stride) * 0.12;
    this.visual.userData.body.position.y = bounce;
    this.visual.userData.body.rotation.z = stride * 0.012;
    this.visual.userData.head.rotation.y = Math.sin(this.walkPhase * 0.5) * 0.035 * blend;
    this.visual.userData.skirt.rotation.y = stride * 0.018;
    for (let index = 0; index < this.visual.userData.hairLocks.length; index += 1) {
      const lock = this.visual.userData.hairLocks[index];
      const offset = index - 3;
      lock.rotation.x = 0.045 + Math.abs(stride) * 0.06;
      lock.rotation.z = (offset / 3) * 0.08 + Math.sin(this.walkPhase * 0.72 + index * 0.35) * 0.025 * blend;
    }
  }

  rotateVrmBone(name, x, y, z) {
    const data = this.vrmBones?.[name];
    if (!data) return;
    data.bone.quaternion.copy(data.rest).multiply(new THREE.Quaternion().setFromEuler(new THREE.Euler(x, y, z)));
  }
}

function rotateTowards(current, target, maxDelta) {
  let difference = ((target - current + Math.PI) % (Math.PI * 2)) - Math.PI;
  if (difference < -Math.PI) difference += Math.PI * 2;
  return current + THREE.MathUtils.clamp(difference, -maxDelta, maxDelta);
}
