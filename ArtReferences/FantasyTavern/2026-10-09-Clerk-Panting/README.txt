점원 떨림·거친 호흡 애니메이션

Clerk-Panting-Preview.gif: 512x512 반복 미리보기. 픽셀 확인을 위한 2배 확대이며 어두운 배경은 미리보기에만 포함.
Clerk-Panting.png: 투명 256x256 APNG, 8프레임, 총 640ms 반복. 정적 PNG 뷰어에서는 첫 프레임만 보일 수 있음.
Frames/: Unity용 투명 256x256 개별 PNG 8장.
Clerk-Panting-Sheet.png: 4열 2행, 1024x512. 왼쪽에서 오른쪽, 위에서 아래 순서.
Animation.json: 프레임 시간, 기준선, 권장 피벗 등 설정.
Unity 사용 시 Sprite (2D and UI), Point 필터, 압축 없음, Mipmap 없음. 시트는 Grid By Cell Size 256x256, Pivot Custom (0.5, 0.0625) 기준. 이번 작업은 리소스 제작이며 씬/Animator 적용은 포함하지 않음.
생성 도구의 프레임 간 머리/의상 윤곽 변화가 있어 최종 출시용에는 추가 정리가 필요함. 발 기준선과 전체 배율을 통일했으나 완전한 동일 실루엣을 보장하는 수작업 프레임은 아님.
Prompt.txt: 내장 imagegen 생성 프롬프트. Assemble.py: 프레임 분할, 공통 배율/기준선 정렬 및 반복 파일 조립.
