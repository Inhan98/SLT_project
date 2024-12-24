clear
close all
clc


% MATLAB 서버 코드 예시 (tcpserver 사용)
server = tcpserver("0.0.0.0", 4504);  % 서버를 30000 포트로 실행
disp('Server ready, waiting for connection...');

while true
    if server.NumBytesAvailable > 0
        data = read(server, server.NumBytesAvailable, "string");  % 데이터 읽기
        disp(['Received: ', data]);

        % 데이터를 ','로 구분하여 소리 유형과 각도 분리
        splitData = split(data, ",");
        soundType = splitData(1);  % 소리 유형
        angle = splitData(2);      % 각도

        % 소리 유형에 따라 다른 소리 재생
        if strcmp(soundType, 'Personalized')
            disp(['Playing Personalized sound at angle: ', angle]);
            [y,fs] = audioread(['./BY/ind/BY_ind_',char(angle),'_v2.wav']);
            sound(y,fs);  
        elseif strcmp(soundType, 'Generic')
            disp(['Playing Generic sound at angle: ', angle]);
            [y,fs] = audioread(['./BY/HATS/BY_HATS_',char(angle),'_v2.wav']);
            sound(y,fs);  
        elseif strcmp(soundType, 'Unrelated')
            disp(['Playing Unrelated sound at angle: ', angle]);
            [y,fs] = audioread(['./BY/pred//BY_pred_',char(angle),'_v2.wav']);
            sound(y,fs); 
        else
            disp('Unknown sound type received.');
        end
    end
    pause(0.1);  % 과도한 CPU 사용을 막기 위한 잠깐의 대기
end
