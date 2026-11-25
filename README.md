# Konseki — Three.js

조용한 일본 주택가의 밤을 배경으로 한 1인칭 미행 게임입니다. Unity를 사용하지 않고 Three.js와 Vite로 실행합니다.

## 실행

macOS에서는 `게임실행.command`를 더블클릭하면 서버와 브라우저가 함께 실행됩니다.

터미널에서 직접 실행하려면:

```bash
npm install
npm run dev
```

터미널에 표시되는 로컬 주소를 브라우저에서 연 뒤 `추적 시작`을 누릅니다.

## 조작

- `W A S D`: 천천히 걷기
- `마우스`: 시점 이동
- `Esc`: 일시 정지 및 마우스 해제
- `R`: 다시 시작

## 타겟 캐릭터

게임은 `public/models/woman-v4-walk.glb`의 V4 캐릭터와 `Walk`/`Idle` 애니메이션을 사용합니다. 원본 모델과 참고 이미지는 `artifacts/new-woman-v4/`에 있습니다. 아래 명령으로 게임용 모델을 다시 만들 수 있습니다.

```bash
blender -b -t 4 --factory-startup --python-exit-code 1 -P tools/rig_woman_v4.py
```

## 성능 설정

- 렌더링 45 FPS 제한
- 백그라운드 탭 렌더링 정지
- 기기 픽셀 비율 최대 1.25
- 그림자 비활성화
- `powerPreference: low-power`
