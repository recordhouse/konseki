import * as THREE from 'three';

const palette = {
  asphalt: 0x26343b,
  concrete: 0x465158,
  wallDark: 0x35434a,
  wallMid: 0x4a5756,
  wallWarm: 0x62594f,
  roof: 0x27333c,
  fence: 0x303b40,
  windowWarm: 0xffce83,
  windowCool: 0x8cbcd1,
};

const standard = (color, roughness = 0.9, metalness = 0) => new THREE.MeshStandardMaterial({ color, roughness, metalness });
const basic = (color) => new THREE.MeshBasicMaterial({ color });

function addBox(scene, size, position, boxMaterial, colliders, collision = false) {
  const object = new THREE.Mesh(new THREE.BoxGeometry(size.x, size.y, size.z), boxMaterial);
  object.position.copy(position);
  scene.add(object);
  if (collision) colliders.push(new THREE.Box3().setFromObject(object));
  return object;
}

function addRoad(scene, x, z, width, length, rotation = 0) {
  const road = addBox(
    scene,
    new THREE.Vector3(width, 0.08, length),
    new THREE.Vector3(x, -0.05, z),
    standard(palette.asphalt, 0.96),
    [],
  );
  road.rotation.y = rotation;
}

function createHouse(scene, colliders, x, z, width, depth, floors, tone, facing = 1) {
  const height = floors * 2.35;
  const group = new THREE.Group();
  group.position.set(x, 0, z);
  scene.add(group);
  const body = new THREE.Mesh(new THREE.BoxGeometry(width, height, depth), standard(tone, 0.92));
  body.position.y = height * 0.5;
  group.add(body);
  const roof = new THREE.Mesh(new THREE.BoxGeometry(width + 0.35, 0.28, depth + 0.35), standard(palette.roof, 0.82));
  roof.position.y = height + 0.12;
  group.add(roof);

  const streetZ = facing * (depth * 0.5 + 0.015);
  const windowMaterial = basic((Math.floor(Math.abs(x + z)) % 3) === 0 ? palette.windowWarm : palette.windowCool);
  for (let floor = 0; floor < floors; floor += 1) {
    const y = 1.25 + floor * 2.25;
    for (const offset of [-width * 0.24, width * 0.24]) {
      const window = new THREE.Mesh(new THREE.PlaneGeometry(0.72, 0.72), windowMaterial);
      window.position.set(offset, y, streetZ);
      window.rotation.y = facing < 0 ? 0 : Math.PI;
      group.add(window);
    }
  }
  const door = new THREE.Mesh(new THREE.PlaneGeometry(0.72, 1.55), standard(0x29353c, 0.75));
  door.position.set(0, 0.8, streetZ + facing * 0.004);
  door.rotation.y = facing < 0 ? 0 : Math.PI;
  group.add(door);
  group.updateMatrixWorld(true);
  colliders.push(new THREE.Box3().setFromObject(body).expandByScalar(0.08));
}

function createStreetLamp(scene, x, z, warm = true) {
  const pole = new THREE.Group();
  pole.position.set(x, 0, z);
  scene.add(pole);
  const poleMaterial = standard(0x252e33, 0.5, 0.45);
  const stem = new THREE.Mesh(new THREE.CylinderGeometry(0.055, 0.07, 4.7, 8), poleMaterial);
  stem.position.y = 2.35;
  pole.add(stem);
  const lamp = new THREE.Mesh(new THREE.SphereGeometry(0.16, 10, 8), basic(warm ? 0xffd596 : 0xa8d1e5));
  lamp.position.y = 4.45;
  pole.add(lamp);
  const light = new THREE.PointLight(warm ? 0xffbf73 : 0x95c9e0, 5.2, 13, 1.7);
  light.position.y = 4.25;
  pole.add(light);
}

function createUtilityPole(scene, x, z) {
  const group = new THREE.Group();
  group.position.set(x, 0, z);
  scene.add(group);
  const poleMaterial = standard(0x2d3334, 0.82);
  const stem = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.13, 7.2, 8), poleMaterial);
  stem.position.y = 3.6;
  group.add(stem);
  const cross = new THREE.Mesh(new THREE.BoxGeometry(1.6, 0.1, 0.12), poleMaterial);
  cross.position.y = 6.55;
  group.add(cross);
}

function createWire(scene, points) {
  const curve = new THREE.CatmullRomCurve3(points.map(([x, y, z]) => new THREE.Vector3(x, y, z)));
  scene.add(new THREE.Mesh(new THREE.TubeGeometry(curve, 24, 0.012, 5, false), basic(0x111619)));
}

function createVendingMachine(scene, colliders, x, z, rotation = 0) {
  const group = new THREE.Group();
  group.position.set(x, 0, z);
  group.rotation.y = rotation;
  scene.add(group);
  const body = new THREE.Mesh(new THREE.BoxGeometry(1.05, 2, 0.62), standard(0x4c687a, 0.42, 0.1));
  body.position.y = 1;
  group.add(body);
  const face = new THREE.Mesh(new THREE.PlaneGeometry(0.86, 1.5), basic(0xc8e7ee));
  face.position.set(0, 1.12, 0.316);
  group.add(face);
  for (let row = 0; row < 3; row += 1) {
    for (let col = 0; col < 4; col += 1) {
      const item = new THREE.Mesh(new THREE.PlaneGeometry(0.12, 0.22), basic([0xd86a59, 0x68a0ba, 0xe4bd6f][row]));
      item.position.set(-0.29 + col * 0.19, 1.55 - row * 0.34, 0.322);
      group.add(item);
    }
  }
  group.updateMatrixWorld(true);
  colliders.push(new THREE.Box3().setFromObject(body));
}

export function buildWorld(scene) {
  const colliders = [];
  scene.background = new THREE.Color(0x0b1621);
  scene.fog = new THREE.FogExp2(0x102334, 0.014);
  scene.add(new THREE.HemisphereLight(0xa7c4df, 0x25313a, 1.35));
  const moon = new THREE.DirectionalLight(0xb8d2e5, 1.65);
  moon.position.set(-12, 20, -8);
  scene.add(moon);

  addBox(scene, new THREE.Vector3(70, 0.2, 105), new THREE.Vector3(0, -0.16, 46), standard(0x202b31, 1), colliders);
  addRoad(scene, 0, 45, 7.2, 94);
  addRoad(scene, -10.5, 26, 4.8, 26, Math.PI * 0.5);
  addRoad(scene, -18, 36, 4.8, 22);
  addRoad(scene, -9, 46, 4.8, 22, Math.PI * 0.5);
  addRoad(scene, 11, 62, 4.8, 26, Math.PI * 0.5);
  addRoad(scene, 22, 72, 4.8, 22);
  addRoad(scene, 11, 82, 4.8, 26, Math.PI * 0.5);

  const curb = standard(palette.concrete, 0.95);
  for (const x of [-4.05, 4.05]) {
    addBox(scene, new THREE.Vector3(0.6, 0.18, 96), new THREE.Vector3(x, 0, 45), curb, colliders);
  }

  const tones = [palette.wallDark, palette.wallMid, palette.wallWarm];
  const mainHouseZ = [7, 17, 36, 54, 72, 89];
  for (let i = 0; i < mainHouseZ.length; i += 1) {
    const z = mainHouseZ[i];
    createHouse(scene, colliders, -8.2, z, 6.4, 6.6, i % 3 === 0 ? 2 : 1, tones[i % 3], 1);
    createHouse(scene, colliders, 8.2, z + 3.5, 6.2, 6.4, i % 2 === 0 ? 2 : 1, tones[(i + 1) % 3], -1);
  }
  createHouse(scene, colliders, -18, 18.5, 6.5, 6.2, 2, palette.wallWarm, 1);
  createHouse(scene, colliders, -25.2, 31.5, 6.2, 7.2, 1, palette.wallMid, -1);
  createHouse(scene, colliders, -18, 52.5, 6.5, 6.5, 2, palette.wallDark, -1);
  createHouse(scene, colliders, 22, 54.2, 6.4, 6.3, 2, palette.wallWarm, 1);
  createHouse(scene, colliders, 28.5, 68.5, 6.1, 7.1, 1, palette.wallMid, -1);
  createHouse(scene, colliders, 22, 88.8, 6.5, 6.6, 2, palette.wallDark, -1);

  for (const [x, z, warm] of [
    [-3.3, 12, true], [3.2, 25, false], [-3.2, 41, true], [3.2, 58, true],
    [-3.2, 77, false], [-16, 27, true], [-19.8, 45, false], [20, 63, true], [23.8, 82, true],
  ]) createStreetLamp(scene, x, z, warm);

  const poles = [[-3.7, 5], [3.7, 20], [-3.7, 38], [3.7, 56], [-3.7, 74], [3.7, 91]];
  for (const [x, z] of poles) createUtilityPole(scene, x, z);
  for (let i = 0; i < poles.length - 1; i += 1) {
    const [x1, z1] = poles[i];
    const [x2, z2] = poles[i + 1];
    createWire(scene, [[x1, 6.6, z1], [(x1 + x2) * 0.5, 5.95, (z1 + z2) * 0.5], [x2, 6.6, z2]]);
  }

  createVendingMachine(scene, colliders, 3.45, 34, -Math.PI * 0.5);
  const wallMaterial = standard(palette.fence, 0.96);
  for (const [x, z, sx, sz] of [
    [-4.7, 30, 0.18, 7], [4.7, 47, 0.18, 8], [-10, 23.2, 8, 0.16],
    [-22, 46.8, 7, 0.16], [14, 59.3, 8, 0.16], [18, 84.7, 7, 0.16],
  ]) addBox(scene, new THREE.Vector3(sx, 1.05, sz), new THREE.Vector3(x, 0.52, z), wallMaterial, colliders, true);

  addBox(scene, new THREE.Vector3(5.5, 3.8, 0.45), new THREE.Vector3(0, 1.9, 93), standard(0x263239, 0.9), colliders, true);
  const door = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 2.6), basic(0xd8bd83));
  door.position.set(0, 1.35, 92.77);
  scene.add(door);
  return { colliders };
}

export const targetRoute = [
  [0, 10], [0, 21], [-8, 26], [-18, 26], [-18, 41], [-10, 46], [0, 46],
  [0, 57], [9, 62], [22, 62], [22, 77], [13, 82], [0, 82], [0, 91],
];
