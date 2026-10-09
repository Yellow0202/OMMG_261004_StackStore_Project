외곽선과 팔레트를 정리한 도트 호흡 수정본

기존 작은 호흡과 짧은 떨림의 방향으로 16프레임 새로 생성. 프레임 그림을 축소하지 않고 원래 셀의 픽셀 크기로 분리해 352x352 투명 캔버스에 정렬.
외곽 정리: 알파 224 미만 제거, 가장 큰 연결 실루엣만 남김, 외곽 경계는 짙은 윤곽색으로 통일. 16색 팔레트와 이진 알파 적용. 정수배 확대 확인용 이미지 포함.
Clerk-Clean-Preview.gif: 352x352 무음 미리보기. 16프레임, 프레임당 100ms, 1.6초 반복.
Clerk-Clean.png: 투명 APNG. Frames/: 투명 PNG 16장. Clerk-Clean-Sheet.png: 4열 4행 1408x1408.
Frame-00-Preview-2x.png: 첫 프레임 2배 확대. Generated-Sheet.png는 정리 전 원본이며 최종 리소스가 아님.
Animation.json: 크기/시간/피벗. Unity Point 필터, 압축 없음, 밉맵 없음 권장.
Clean.ps1은 Windows PowerShell 5.1에서 실행. Assemble.py는 프레임과 반복 파일 조립 및 크기/색상 수/알파 검증.
게임 씬 및 Animator 적용 없음. 생성 프레임 사이 작은 실루엣 변화는 완전한 수작업 애니메이션과 차이가 있어 후속 정리가 필요함.
