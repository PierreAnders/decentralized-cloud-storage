
<template>
  <div ref="sceneContainer" class="fixed top-0 left-0 w-full h-screen z-0"></div>
</template>

<script setup>
import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls';

const sceneContainer = ref(null);

const initThreeJS = () => {
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 1, 4000);
  camera.position.set(0, 0, 800);

  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setClearColor(0x111114);
  sceneContainer.value.appendChild(renderer.domElement);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.25;
  controls.screenSpacePanning = false;
  controls.maxPolarAngle = Math.PI / 2;

  const group = new THREE.Group();
  scene.add(group);

  const r = 800;
  const rHalf = r / 2;

  const maxParticleCount = 110;
  let particleCount = 0;
  const particlesData = [];

  const positions = new Float32Array(maxParticleCount * 3 * maxParticleCount);
  const colors = new Float32Array(maxParticleCount * 3 * maxParticleCount);

  const pMaterial = new THREE.PointsMaterial({
    color: 0xFFFFFF,
    size: 3,
    blending: THREE.AdditiveBlending,
    transparent: true,
    sizeAttenuation: false
  });

  const particles = new THREE.BufferGeometry();
  const particlePositions = new Float32Array(maxParticleCount * 3);
  particles.setAttribute('position', new THREE.BufferAttribute(particlePositions, 3).setUsage(THREE.DynamicDrawUsage));
  particles.setDrawRange(0, particleCount);
  const pointCloud = new THREE.Points(particles, pMaterial);
  group.add(pointCloud);

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3).setUsage(THREE.DynamicDrawUsage));
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3).setUsage(THREE.DynamicDrawUsage));
  geometry.computeBoundingSphere();
  geometry.setDrawRange(0, 0);

  const material = new THREE.LineBasicMaterial({
    vertexColors: true,
    blending: THREE.AdditiveBlending,
    transparent: true,
    opacity: 0.6
  });

  const linesMesh = new THREE.LineSegments(geometry, material);
  group.add(linesMesh);

  let frameCount = 0;

  const addParticles = () => {
    if (particleCount < maxParticleCount && frameCount % 10 === 0) {
      const startIndex = particleCount * 3;
      for (let i = particleCount; i < Math.min(particleCount + 5, maxParticleCount); i++) {
        const x = Math.random() * r - rHalf;
        const y = Math.random() * r - rHalf;
        const z = Math.random() * r - rHalf;

        particlePositions[i * 3] = x;
        particlePositions[i * 3 + 1] = y;
        particlePositions[i * 3 + 2] = z;

        particlesData.push({
          velocity: new THREE.Vector3(-1 + Math.random() * 2, -1 + Math.random() * 2, -1 + Math.random() * 2),
          numConnections: 0
        });
      }
      particleCount += 5;
      particles.setDrawRange(0, particleCount);
    }
  };

  const animate = () => {
    requestAnimationFrame(animate);

    addParticles();

    let vertexpos = 0;
    let colorpos = 0;
    let numConnected = 0;

    for (let i = 0; i < particleCount; i++) particlesData[i].numConnections = 0;

    positions.fill(0);
    colors.fill(0);

    for (let i = 0; i < particleCount; i++) {
      const particleData = particlesData[i];

      const speedFactor = 0.3;
      particlePositions[i * 3] += particleData.velocity.x * speedFactor;
      particlePositions[i * 3 + 1] += particleData.velocity.y * speedFactor;
      particlePositions[i * 3 + 2] += particleData.velocity.z * speedFactor;

      if (particlePositions[i * 3 + 1] < -rHalf || particlePositions[i * 3 + 1] > rHalf)
        particleData.velocity.y = -particleData.velocity.y;

      if (particlePositions[i * 3] < -rHalf || particlePositions[i * 3] > rHalf)
        particleData.velocity.x = -particleData.velocity.x;

      if (particlePositions[i * 3 + 2] < -rHalf || particlePositions[i * 3 + 2] > rHalf)
        particleData.velocity.z = -particleData.velocity.z;

      for (let j = i + 1; j < particleCount; j++) {
        const particleDataB = particlesData[j];
        const dx = particlePositions[i * 3] - particlePositions[j * 3];
        const dy = particlePositions[i * 3 + 1] - particlePositions[j * 3 + 1];
        const dz = particlePositions[i * 3 + 2] - particlePositions[j * 3 + 2];
        const dist = Math.sqrt(dx * dx + dy * dy + dz * dz);

        if (dist < 150) {
          particleData.numConnections++;
          particleDataB.numConnections++;

          const alpha = 1.0 - dist / 150;

          positions[vertexpos++] = particlePositions[i * 3];
          positions[vertexpos++] = particlePositions[i * 3 + 1];
          positions[vertexpos++] = particlePositions[i * 3 + 2];

          positions[vertexpos++] = particlePositions[j * 3];
          positions[vertexpos++] = particlePositions[j * 3 + 1];
          positions[vertexpos++] = particlePositions[j * 3 + 2];

          colors[colorpos++] = alpha;
          colors[colorpos++] = alpha;
          colors[colorpos++] = alpha;

          colors[colorpos++] = alpha;
          colors[colorpos++] = alpha;
          colors[colorpos++] = alpha;

          numConnected++;
        }
      }
    }

    linesMesh.geometry.setDrawRange(0, numConnected * 2);
    linesMesh.geometry.attributes.position.needsUpdate = true;
    linesMesh.geometry.attributes.color.needsUpdate = true;

    pointCloud.geometry.attributes.position.needsUpdate = true;

    const rotationSpeed = 0.001;
    group.rotation.y += rotationSpeed;

    // controls.update();
    renderer.render(scene, camera);

    frameCount++;
  };

  animate();

  window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
  });

  onBeforeUnmount(() => {
    window.removeEventListener('resize', () => {});
    renderer.dispose();
  });
};

onMounted(initThreeJS);
</script>
