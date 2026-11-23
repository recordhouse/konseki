import * as THREE from 'three';
import './style.css';
import { FirstPersonPlayer } from './player.js';
import { TargetActor } from './target.js';
import { buildWorld, targetRoute } from './world.js';

const canvas = document.querySelector('#game-canvas');
const startScreen = document.querySelector('#start-screen');
const resultScreen = document.querySelector('#result-screen');
const startButton = document.querySelector('#start-button');
const restartButton = document.querySelector('#restart-button');
const distanceValue = document.querySelector('#distance-value');
const distanceBar = document.querySelector('#distance-bar');
const suspicionValue = document.querySelector('#suspicion-value');
const suspicionBar = document.querySelector('#suspicion-bar');
const statusPill = document.querySelector('#status-pill');
const objectiveText = document.querySelector('#objective-text');
const assetStatus = document.querySelector('#asset-status');
const resultTitle = document.querySelector('#result-title');
const resultMessage = document.querySelector('#result-message');
const resultKicker = document.querySelector('#result-kicker');

const renderer = new THREE.WebGLRenderer({
  canvas,
  antialias: true,
  alpha: false,
  powerPreference: 'low-power',
});
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.08;
renderer.shadowMap.enabled = false;

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(66, 1, 0.05, 125);
const { colliders } = buildWorld(scene);
const player = new FirstPersonPlayer(camera, canvas, colliders);
const target = new TargetActor(scene, targetRoute, (message) => {
  assetStatus.textContent = message;
});

const game = { phase: 'ready', suspicion: 0, lostTime: 0, safeTime: 0 };

function resize() {
  const width = window.innerWidth;
  const height = window.innerHeight;
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.25));
  renderer.setSize(width, height, false);
  camera.aspect = width / Math.max(height, 1);
  camera.updateProjectionMatrix();
}

function beginGame() {
  if (game.phase === 'won' || game.phase === 'lost') resetGame();
  game.phase = 'playing';
  player.setEnabled(true);
  startScreen.classList.remove('visible');
  resultScreen.classList.remove('visible');
  canvas.requestPointerLock();
}

function resetGame() {
  game.phase = 'ready';
  game.suspicion = 0;
  game.lostTime = 0;
  game.safeTime = 0;
  player.reset();
  target.reset();
  updateHud(8);
}

function endGame(won, reason) {
  game.phase = won ? 'won' : 'lost';
  player.setEnabled(false);
  if (document.pointerLockElement === canvas) document.exitPointerLock();
  resultKicker.textContent = won ? 'TAIL COMPLETE' : 'TAIL LOST';
  resultTitle.textContent = won ? '흔적을 따라잡았습니다' : '추적에 실패했습니다';
  resultMessage.textContent = reason;
  resultScreen.classList.add('visible');
}

function updateGame(delta) {
  if (game.phase !== 'playing') return;
  const playerGround = new THREE.Vector3(player.position.x, 0, player.position.z);
  const targetGround = target.root.position.clone().setY(0);
  const distance = playerGround.distanceTo(targetGround);
  const toPlayer = playerGround.clone().sub(targetGround).normalize();
  const targetForward = new THREE.Vector3(0, 0, 1).applyQuaternion(target.root.quaternion);
  const targetCanSee = targetForward.dot(toPlayer) > 0.38 && distance < 10.5;

  let suspicionRate = -7;
  if (distance < 2.2) suspicionRate = 34;
  else if (distance < 3.5) suspicionRate = 13;
  if (targetCanSee) suspicionRate += 24;
  game.suspicion = THREE.MathUtils.clamp(game.suspicion + suspicionRate * delta, 0, 100);

  if (distance > 21) game.lostTime += delta;
  else game.lostTime = Math.max(0, game.lostTime - delta * 1.8);

  const safe = distance >= 4.2 && distance <= 13 && game.suspicion < 65;
  game.safeTime = safe ? game.safeTime + delta : Math.max(0, game.safeTime - delta * 0.5);
  updateHud(distance, targetCanSee);

  if (game.suspicion >= 100) endGame(false, '타겟이 뒤따라오는 기척을 확신했습니다. 거리를 조금 더 두세요.');
  else if (game.lostTime > 7) endGame(false, '타겟을 너무 오래 놓쳤습니다. 골목 모퉁이에서도 시야를 유지하세요.');
  else if (target.finished) {
    if (safe && game.safeTime > 3) endGame(true, '들키지 않고 목적지까지 미행했습니다.');
    else endGame(false, '목적지에는 도착했지만 마지막 거리를 유지하지 못했습니다.');
  }
}

function updateHud(distance, targetCanSee = false) {
  distanceValue.textContent = `${distance.toFixed(1)} m`;
  distanceBar.style.width = `${THREE.MathUtils.clamp((distance / 22) * 100, 0, 100)}%`;
  suspicionValue.textContent = `${Math.round(game.suspicion)}%`;
  suspicionBar.style.width = `${game.suspicion}%`;

  let state = 'safe';
  let label = '안전 거리';
  let objective = '타겟과 거리를 유지하세요';
  if (targetCanSee) {
    state = 'danger'; label = '시야에 노출됨'; objective = '시선을 피하고 골목을 이용하세요';
  } else if (distance < 3.5) {
    state = 'danger'; label = '너무 가까움'; objective = '속도를 줄여 거리를 벌리세요';
  } else if (distance > 17) {
    state = 'warning'; label = '놓칠 위험'; objective = '타겟이 사라지기 전에 따라가세요';
  } else if (game.suspicion > 60) {
    state = 'warning'; label = '의심 증가'; objective = '잠시 멈춰 의심을 낮추세요';
  }

  statusPill.className = `status-pill ${state}`;
  statusPill.querySelector('span').textContent = label;
  objectiveText.textContent = objective;
}

startButton.addEventListener('click', beginGame);
restartButton.addEventListener('click', beginGame);
canvas.addEventListener('click', () => {
  if (game.phase === 'playing' && document.pointerLockElement !== canvas) canvas.requestPointerLock();
});
document.addEventListener('pointerlockchange', () => {
  const locked = document.pointerLockElement === canvas;
  player.setEnabled(locked && game.phase === 'playing');
  if (!locked && game.phase === 'playing') startScreen.classList.add('visible');
  else if (locked) startScreen.classList.remove('visible');
});
window.addEventListener('keydown', (event) => {
  if (event.code === 'KeyR') {
    resetGame();
    beginGame();
  }
});
window.addEventListener('resize', resize);
resize();
updateHud(8);

// Static browser capture mode for visual QA. Normal players never enter this path.
if (new URLSearchParams(window.location.search).has('preview')) {
  game.phase = 'playing';
  player.setEnabled(false);
  startScreen.classList.remove('visible');
  resultScreen.classList.remove('visible');
}

renderer.render(scene, camera);

let accumulator = 0;
let previousTime = performance.now();
const frameInterval = 1 / 45;

function animate(timestamp) {
  requestAnimationFrame(animate);
  const currentTime = Number.isFinite(timestamp) ? timestamp : performance.now();
  const rawDelta = Math.min(Math.max((currentTime - previousTime) / 1000, 0), 0.08);
  previousTime = currentTime;
  if (document.hidden) return;
  accumulator += rawDelta;
  if (accumulator < frameInterval) return;
  const delta = Math.min(accumulator, 0.05);
  accumulator = 0;
  const active = game.phase === 'playing';
  player.update(delta);
  target.update(delta, active);
  updateGame(delta);
  renderer.render(scene, camera);
}

animate();
