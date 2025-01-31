#include <iostream>
#include <filesystem>
#include<windows.h>
#include <thread>
#include <regex>

using namespace std;
using namespace std::filesystem;


/// <summary>
/// 返回一个当前目录下发现的目录的路径
/// </summary>
/// <param name="dirPath">当前目录</param>
/// <returns></returns>
static string enterDirPath(const string dirPath) {
    directory_iterator fileList(dirPath);
    for (auto& file : fileList) {
        string fp = file.path().string();
        if (is_directory(fp)) {
            return fp;
        }
    }
	return dirPath;//如果未找到则返回原路径
}
/// <summary>
/// 遍历文件夹内的所有文件，会进行深度遍历，但不会将目录记录在内
/// </summary>
/// <param name="dirPath">遍历的文件路径</param>
/// <returns></returns>
static vector<string> getDirAllFile(const std::string dirPath)
{
    directory_iterator fileList(dirPath);
    vector<string> fileListStr;
    for (auto& file : fileList)
    {
         string fp = file.path().string();
         if (is_directory(fp)) {
             for (auto& fp2 : getDirAllFile(fp)) {
                 fileListStr.push_back(fp2);
            }
         }
         else {
             fileListStr.push_back(fp);
             cout << "找到的文件: " << fp << endl;
         }
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
/// <summary>
/// 移动文件函数，不支持移动文件夹，不要在文其中包含文件夹路径
/// </summary>
/// <param name="oldPath"></param>
/// <param name="newPath"></param>
/// <returns></returns>
static bool moveFile(string oldPath,string newPath) {
    try {
        if (exists(oldPath)) {
            path dir_path = path(newPath).parent_path();
            if (!exists(dir_path))//判断路径中的文件夹是否存在
            {
                try {
                    create_directory(dir_path);//否则创建一个目录
                    cout << "新建目录: +> " << dir_path.string() << endl;
                }
                catch (...) { cout << "新建目录失败(发生错误): /> " << dir_path.string() << endl; }
            }
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
/// 5: 进入目录嵌套的深度，为0时禁用<br/>
/// </param>
/// <returns></returns>
int main(int argc, char* argv[])
{
    try {
		cout << "等待" << argv[1] << "毫秒后开始执行..." << endl;
        Sleep(stoi(argv[1]));//执行前等待
        string sourceDir = argv[2];
        try {
            for (int i = 0; i < stoi(argv[5]); i++) {
                string tmp = enterDirPath(sourceDir);
				if (tmp != sourceDir) {
					sourceDir = tmp;
                    cout << "进入嵌套目录: |> " << sourceDir << endl;	
                }
                else {
                    cout << "进入嵌套目录失败(未找到目录) |/> " << sourceDir << "\\?" << endl;
                    return 0;
                }
            }
		}
        catch (...) { cout << "进入嵌套目录失败(发生错误) |/> " << sourceDir << "|?" << endl; return 0; }

        cout << "开始检查文件..." << endl;
        vector<string> moveFiles = getDirAllFile(sourceDir);
        int restartNum = 0;
    rego:;
        if (moveFiles.size() > 0) {
            cout << "开始执行文件操作..." << endl;
            for (auto& file : moveFiles) {
                string relativePath = file;
                relativePath.replace(0, int(canonical(sourceDir).string().length()), "");
                moveFile(file, argv[3] + relativePath);
            }
        }
        cout << "验证文件..." << endl;
        moveFiles = getDirAllFile(sourceDir);
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

        if (string(argv[4]) != "NULL") {
            cout << "尝试启动: " << argv[4] << endl;
            WinExec(argv[4], SW_SHOW);
        }

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

