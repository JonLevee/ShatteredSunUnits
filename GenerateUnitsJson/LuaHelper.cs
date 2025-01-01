using NLua;
using System.Text;

namespace GenerateUnitsJson
{
    public class LuaHelper
    {
        public static IDisposable GetLuaTable(string luaRoot, string relativePath, string tableName, out LuaTable table)
        {
            var lua = new Lua();
            var fullPath = Path.Combine(luaRoot, relativePath);
            lua.State.Encoding = Encoding.UTF8;
            lua.DoFile(fullPath);
            table = (LuaTable)lua[tableName];
            return new Disposable(lua.Dispose);
        }

    }
}

