## VR 시뮬레이션
#### 1. VR 장비를 쓰지 않고 테스트하고 싶을 때
(1) 아래 그림에서 **Hierarchy 창에서 XR Device Simulator** 클릭
(2) 그림 오른쪽 체크 표시 클릭
(3) **Play** 버튼을 누르면 실행 가능합니다!

# 주의. VR을 착용하고 시뮬레이션 테스트를 할 때는, 체크 표시를 무조건 해제해야 합니다! 체크 해제하지 않으면 화면이 움직이지 않습니다.

![image](https://github.com/user-attachments/assets/d6a29fd1-0c18-4a68-9499-897269abc8ca)



## Hierarchy
- 프로그램 실행을 위한 스피커, 벽, 설치물 등 각종 오브젝트를 관리할 때 필요한 창입니다.
- 가장 중요한 오브젝트는 **speakersetup**입니다.
![image](https://github.com/user-attachments/assets/324126f6-b1c8-44cb-8bab-f908cb10482a)

### speakersetup
1. speakersetup의 하위 계층에 총 23개의 스피커가 부착되어 있습니다. 이때 speakersetup은 Speaker_Boombox_Num들을 관리하는 Class 역할을 담당한다고 생각하면 이해하기 쉽습니다. 
	1. 예를 들어 speakersetup에서 다음 Trial에 재생할 speaker와 sound를 결정하고 하위 계층의 speaker_boombox에 명령을 보냅니다. 그 후, 해당 speaker_boombox는 MATLAB과 송신하여 소리를 재생합니다.

![image](https://github.com/user-attachments/assets/182d436b-cf7b-4016-a43d-d7c29925414b)

2. **speakersetup**을 클릭하면 오른쪽에 아래 그림처럼 *inspector* 창이 나타납니다. 이를 활용해 오브젝트에 기능과 특성을 부여할 수 있습니다.

![image](https://github.com/user-attachments/assets/167a58f4-c4b1-4db9-860e-4faf948f7600)
1. speakersetup의 inspector를 확인하면 GameManager (Script)라고 하는 cs script 기반으로 각종 기능이 구현되었음을 확인할 수 있습니다
	1. 이 부분에 대해서는 딱히 수정할 필요가 없습니다. 아래 변수들의 역할과 기능에 대해 궁금하다면 **GameManager.cs** 파일의 주석을 참조해주세요.

![image](https://github.com/user-attachments/assets/d21599e2-d740-4575-b716-2eed0570150d)


### Speaker_BoomBox_Num
- **Speaker_Boombox_Num** 는 Speaker Object임. 포인터를 통해 스피커를 클릭할 때 각종 Reaction을 담당하는 코드.
![image](https://github.com/user-attachments/assets/06b9d121-7ab0-4387-a013-b02087fc7a40)

- 이 오브젝트에서 신경 써야 할 부분은 Speaker ( Script ). 
- 여기서 **Is Fake**를 클릭할 시 이 스피커는 fake 처리되며 소리가 나지 않습니다!
##### 여기서 중요한 점은 MatLab와 통신하면서 sound를 재생할 떄는 Personlaized HRTF, Generic HRTF, Unrelated HRTF에 사운드를 삽입할 필요가 없습니다! 사운드는 매트랩을 통해 재생되기 때문입니다. 나머지는 코드(Speaker.cs) 안의의 주석을 참고하면 됩니다. 
![image](https://github.com/user-attachments/assets/c92b1c33-2440-459e-8d32-23684e5babbc)

### 추가 팁
![image](https://github.com/user-attachments/assets/a973ce3c-1324-442b-8b50-33d9f06e0a83)
- 스피커와 상호 작용하기 위해서는 speaker object의 Select에 위 그림과 같이 설정되어 있어야 합니다. 그렇지 않으면 speaker는 클릭해도 아무 반응을 하지 않습니다.

## Log File 위치 정하기
- Assets > Course Library > Scripts > GameManager.cs
- directoryPath에서 Filepath 정하기

![image](https://github.com/user-attachments/assets/d0d75353-ff2b-4b29-8f45-d2b6b5e41514)
## 매트랩 설정
 Unity 프로그램을 실행하기에 앞서 github project에 있는 **SLT_sound_play.m** 파일을 열고 먼저 실행하면 server가 실행되며 외부 명령을 받을 수 있는 상태가 됩니다! 이후 Unity를 Play하면 다른 설정 건드리지 않고 진행할 수 있습니다.

단 **SLT_sound_play.m**와 동일한 경로에 **BY** 이름의 directory가 있어야 하며 아래 tree처럼 HATS,  ind, pred로 나누어 파일을 넣어두어야 합니다.

매트랩 server의 구조는 내부 코드를 참조하시면 됩니다! 매우 간단한 구조여서 이해하기 어려운 부분은 딱히 없을 것입니다.

![image](https://github.com/user-attachments/assets/16df352a-48d9-43df-85f8-978a8b7b2247)
## 코드 확인 & 수정
![image](https://github.com/user-attachments/assets/ea7c9604-48e1-4f63-84fe-526db9398fbb)
- 위 사진에서처럼 Unity 화면 아래 쪽 Project에서 위 디렉토리를 따라 들어가면 됩니다. GameManager와 Speaker만 확인하면 됩니다!






