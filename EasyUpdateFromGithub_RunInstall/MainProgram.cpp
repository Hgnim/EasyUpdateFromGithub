#include <iostream>
#include <filesystem>
#include<windows.h>

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
            rename(oldPath, newPath);
            cout << "文件变动: " << oldPath << " ->> " << newPath << endl;
            return true;
        }
        else {
            return false;
        }
    }
    catch (...) { return false; }
}
/// <summary>
/// 
/// </summary>
/// <param name="argc"></param>
/// <param name="argv">
/// 0: 默认<br/>
/// 1: 被移动的所有文件所在的目录<br/>
/// 2: 目标目录<br/>
/// 3: 执行安装完后的可执行文件路径，为NULL时禁用<br/>
/// </param>
/// <returns></returns>
int main(int argc, char* argv[])
{
    try {
        cout << "开始检查文件..." << endl;
        vector<string> moveFiles = getDirAllFile(argv[1]);
    rego:;
        if (moveFiles.size() > 0) {
            cout << "开始执行文件操作..." << endl;
            for (auto& file : moveFiles) {
                moveFile(argv[1] + string("\\") + file, argv[2] + string("\\") + file);
            }
        }
        cout << "验证文件..." << endl;
        moveFiles = getDirAllFile(argv[1]);
        if (moveFiles.size() > 0)
        {
            cout << "验证失败，等待1秒后重新尝试执行..." << endl;
            Sleep(1000);
            goto rego;
        }
        cout << "完成!" << endl;

        if (argv[3] != "NULL")
            system(argv[3]);
        system("timeout /t 3");
    }
    catch (...) { 
        cout << "发生错误！" << endl; 
        system("pause");
    }          
}

