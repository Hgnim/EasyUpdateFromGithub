#include <iostream>
#include <filesystem>
#include<windows.h>
#include <thread>

using namespace std;
using namespace std::filesystem;

static vector<string> getDirAllFile(const std::string dirPath)
{
    directory_iterator list(dirPath);
    vector<string> fileListStr;
    for (auto& it : list)
    {
         fileListStr.push_back(it.path().filename().string());
         cout << "找到的文件: " << it.path().filename().string() << endl;
    }
    return fileListStr;	
}
static bool removeFile(string filePath) {
    bool ret;
    try {
        ret = remove(filePath);
        cout << "文件已删除: " << filePath <<endl;
    }
    catch (...) {
        ret = false;
    }
    return ret;
}
static bool moveFile(string oldPath,string newPath) {
    try {
        if (exists(oldPath)) {   
            //path p1 = oldPath;
            //path p2 = newPath;
            //rename(p1,p2);
            if (MoveFileExA(oldPath.c_str(), newPath.c_str(), MOVEFILE_COPY_ALLOWED + MOVEFILE_REPLACE_EXISTING + MOVEFILE_WRITE_THROUGH) == 0)
               goto error;
            cout << "文件变动: " << oldPath << " ->> " << newPath << endl;
            return true;
        error:;
            cout << "文件变动失败(发生错误): " << oldPath << " -/> " << newPath << endl; return false;
        }
        else {
            cout << "文件变动失败(未找到此文件): " << oldPath << " -/> " << newPath << endl;
            return false;
        }
    }
    catch (...) { cout << "文件变动失败(发生错误): " << oldPath << " -/> " << newPath << endl; return false; }   
}
/// <summary>
/// 
/// </summary>
/// <param name="argc"></param>
/// <param name="argv">
/// 参数是必须的，否则程序可能会无法正常运行<br/>
/// 0: 默认<br/>
/// 1: 执行前等待的时间
/// 2: 被移动的所有文件所在的目录<br/>
/// 3: 目标目录<br/>
/// 4: 执行安装完后的可执行文件路径，为NULL时禁用<br/>
/// </param>
/// <returns></returns>
int main(int argc, char* argv[])
{
    try {
        Sleep(stoi(argv[1]));
        cout << "开始检查文件..." << endl;
        vector<string> moveFiles = getDirAllFile(argv[2]);
        int restartNum = 0;
    rego:;
        if (moveFiles.size() > 0) {
            cout << "开始执行文件操作..." << endl;
            for (auto& file : moveFiles) {
                moveFile(argv[2] + string("\\") + file, argv[3] + string("\\") + file);
            }
        }
        cout << "验证文件..." << endl;
        moveFiles = getDirAllFile(argv[2]);
        if (moveFiles.size() > 0)
        {
            if (restartNum < 20) {
                cout << "验证失败，等待1秒后重新尝试执行..." << endl;
                Sleep(1000);
                restartNum++;
                goto rego;
            }
            else {
                cout << "失败次数过多，程序终止" << endl;
                goto pauseProg;
            }
        }
        cout << "完成!" << endl;

        if (string(argv[4]) != "NULL")
            WinExec(argv[4], SW_SHOW);

        system("timeout /t 3");
        return 0;
    pauseProg:;
        system("pause");
    }
    catch (...) { 
        cout << "发生错误！" << endl; 
        system("pause");
    }          
}

