// UDP_Test.cpp : Diese Datei enthält die Funktion "main". Hier beginnt und endet die Ausführung des Programms.
//

#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <iostream>
#include "conio.h"
#include "winsock2.h"
#include "Ws2tcpip.h"
#include "tchar.h"
#include <string> 
#include <windows.h>
#include <sstream>
#include <iomanip>
#include <cstring> 

#pragma comment(lib, "Ws2_32.lib")

using namespace std;

template <typename T>
inline string int_to_hex(T val, size_t width = sizeof(T) * 2)
{
    stringstream ss;
    ss << setfill('0') << setw(width) << hex << (val | 0);
    return ss.str();
}

int main(){
    cout << "UDP Casambi API-Tester\r\n";
    cout << "\u00a92020: Lichtmanufaktur Berlin GmbH\r\n";
    cout << "---------------------------------------\r\n";
    cout << "\r\n";

    string submitted_ip;
    cout << "Please enter IP-Adress (XXX.XXX.XXX.255):";
    cin >> submitted_ip;
    cout << "You have entered: " + submitted_ip + "\r\n";

    int submitted_port;
    cout << "Please enter the used port (10009):";
    cin >> submitted_port;
    cout << "You have entered: " + to_string(submitted_port) + "\r\n";

    int submitted_net_id;
    cout << "Please enter the used net_id (0-255):";
    cin >> submitted_net_id;
    cout << "You have entered: " + to_string(submitted_net_id) + "\r\n";

    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    SOCKET sock;
    sock = socket(AF_INET, SOCK_DGRAM, 0);

    char broadcast = '1';

    if (setsockopt(sock, SOL_SOCKET, SO_BROADCAST, &broadcast, sizeof(broadcast)) < 0){
        cout << "Error in setting Broadcast option\r\n";
        closesocket(sock);
        return 0;
    }

    struct sockaddr_in Recv_addr;
    struct sockaddr_in Sender_addr;

    int len = sizeof(struct sockaddr_in);

    char recvbuff[50];
    int recvbufflen = 50;
    char sendMSG[] = "Broadcast message from Tester\r\n";

    Recv_addr.sin_family = AF_INET;
    Recv_addr.sin_port = htons(submitted_port);
    Recv_addr.sin_addr.s_addr = INADDR_ANY;

    if (bind(sock, (sockaddr*)&Recv_addr, sizeof(Recv_addr)) < 0){
        cout << "Error in BINDING" << WSAGetLastError() << "\r\n";
        _getch();
        closesocket(sock);
        return 0;
    }

    string command_begin;
    command_begin = to_string(submitted_net_id) + ".72.";

    string command_end;
    command_end = "\r\n";

    int casambi_id = 1;

    while (1) {
        for (int i = 0; i < 50; i++) {
            recvbuff[i] = '\0';
        }

        string command;
        command = command_begin + "2.39."+ int_to_hex(casambi_id, 2) + command_end;
        char command_cstr[40];
        strncpy_s(command_cstr, command.c_str(), command.length());

        Recv_addr.sin_addr.s_addr = inet_addr(submitted_ip.c_str());
        if (sendto(sock, command_cstr, strlen(sendMSG) + 1, 0, (sockaddr*)&Recv_addr, sizeof(Sender_addr)) < 0) {
            cout << "Error in Sending." << WSAGetLastError() << "\r\n";
            cout << "Press any key to continue....\r\n";
            _getch();
            closesocket(sock);
            return 0;
        }

        recvfrom(sock, recvbuff, recvbufflen, 0, (sockaddr*)&Sender_addr, &len);

        string recvbuff_string;
        recvbuff_string = string(recvbuff);
        if (recvbuff_string.length() > 15) {
            cout << "Received Message is : " << recvbuff;

            int position;
            string sub_string;
            int net_id_rcv = 255;

            //net_id
            position = recvbuff_string.find(".");
            sub_string = recvbuff_string.substr(0, position);
            net_id_rcv = stoi(sub_string);
            cout << "net_id: " << sub_string << "\r\n";
            recvbuff_string.erase(0, position+1);
            if (submitted_net_id == net_id_rcv) {
                //command_direction
                position = recvbuff_string.find(".");
                sub_string = recvbuff_string.substr(0, position);
                cout << "command_direction: " << sub_string << "\r\n";
                recvbuff_string.erase(0, position + 1);
                //length
                position = recvbuff_string.find(".");
                sub_string = recvbuff_string.substr(0, position);
                cout << "length: " << sub_string << "\r\n";
                string command_length = sub_string;
                recvbuff_string.erase(0, position + 1);
                //command_id
                position = recvbuff_string.find(".");
                sub_string = recvbuff_string.substr(0, position);
                if (sub_string == "39") {
                    cout << "command_id: Node Status\r\n";
                    recvbuff_string.erase(0, position + 1);
                    //unit_id
                    position = recvbuff_string.find(".");
                    sub_string = recvbuff_string.substr(0, position);
                    cout << "Unit_ID: " << sub_string << "\r\n";
                    recvbuff_string.erase(0, position + 1);
                    //Scene
                    position = recvbuff_string.find(".");
                    sub_string = recvbuff_string.substr(0, position);
                    cout << "Scene: " << sub_string << "\r\n";
                    recvbuff_string.erase(0, position + 1);
                    //priority + nodetyp
                    position = recvbuff_string.find(".");
                    sub_string = recvbuff_string.substr(0, position);
                    unsigned char priority_nodetyp = 0;
                    unsigned char priority_nodetyp_tmp = 0;
                    priority_nodetyp = stoi(sub_string, NULL, 16);
                    priority_nodetyp_tmp = priority_nodetyp << 2;
                    priority_nodetyp_tmp = priority_nodetyp_tmp >> 2;
                    cout << "Priority: " << to_string(priority_nodetyp_tmp) << "\r\n";
                    priority_nodetyp_tmp = priority_nodetyp >> 6;
                    cout << "Node_type: " << to_string(priority_nodetyp_tmp) << "\r\n";
                    recvbuff_string.erase(0, position + 1);
                    //condition
                    position = recvbuff_string.find(".");
                    sub_string = recvbuff_string.substr(0, position);
                    cout << "Condition: " << sub_string << "\r\n";
                    recvbuff_string.erase(0, position + 1);
                    //condition
                    position = recvbuff_string.find(".");
                    sub_string = recvbuff_string.substr(0, position);
                    unsigned char online = 0;
                    unsigned char online_tmp = 0;
                    online = stoi(sub_string);
                    online_tmp = online << 7;
                    online_tmp = online_tmp >> 7;
                    cout << "online: " << to_string(online_tmp) << "\r\n";
                    recvbuff_string.erase(0, position + 1);
                }
                else {
                    cout << "command_id: " << sub_string << "\r\n";
                }
            }
        }

        Sleep(2000);
        if (casambi_id < 0xFB) {
            casambi_id++;
        } else {
            cout << "---------------------------------------\r\n";
            casambi_id = 1;
        }
    }

    cout << "\n\n\tpress any key to CONT...";
    _getch();

    closesocket(sock);
    WSACleanup();
}
