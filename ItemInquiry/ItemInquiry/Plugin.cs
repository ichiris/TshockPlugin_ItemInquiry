using System.Numerics;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using NPinyin;
using Terraria.ID;
using Terraria.DataStructures;

using Microsoft.Xna.Framework;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Version = System.Version;


namespace ItemInquiry;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Name => "ItemInquiry";
    public override string Author => "ichiris";
    public override string Description => "快速查找物品存放的箱子位置";
    public override Version Version => new Version(0, 3);

    public Plugin(Main game) : base(game)
    {
    }

    private string helpmsg = "使用/ii <item_name> 以检索物品.";

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command("iteminquiry", OnCommand, "ii"));
    }

    private bool Enable = true;
    private bool Allow_Nolimit = true;
    private int Max_distance = 100;


    private void OnCommand(CommandArgs args)
    {
        // if (args.Parameters.Count == 0) {
        //     args.Player.SendInfoMessage(this.helpmsg);
        // }
        //
        // else if (args.Parameters.Count == 2) {
        //     if (args.Parameters[1].ToLower() == "nolimit" || args.Parameters[1].ToLower() == "nl") {
        //         if (args.Parameters[0] == "none") args.Parameters[0] = "";
        //         chest_finder(args.Parameters[0], args.Player, true, args);
        //     }
        //     else {
        //         args.Player.SendInfoMessage(helpmsg);
        //     }
        // }
        //
        // else if (args.Parameters.Count == 1) {
        //     if (args.Parameters[0] == "none") args.Parameters[0] = "";
        //     chest_finder(args.Parameters[0], args.Player, false, args);
        // }
        // else {
        //     args.Player.SendInfoMessage(this.helpmsg);
        // }

        if (args.Parameters.Count == 0) {
            args.Player.SendInfoMessage(this.helpmsg);
            return;
        }

        if (args.Parameters[0]=="none")args.Parameters[0] = "";
        
        if (args.Parameters.Count == 2)
            if (args.Parameters[1]=="nolimit"||args.Parameters[1]=="nl")
                chest_finder(args.Parameters[0],args.Player,true,args);
            else args.Player.SendInfoMessage(helpmsg);
        
        if (args.Parameters.Count == 1)
            chest_finder(args.Parameters[0],args.Player,false,args);
    }

    private void chest_finder(string target, TSPlayer plr, bool nolimit, CommandArgs args)
    {
        var chests = Main.chest;
        bool is_find = false;
        
        for (int i = 0; i < chests.Length; i++) {
            var cst = chests[i];

            if (cst == null) continue;
            if ((!(Math.Abs(cst.x - plr.TileX) < this.Max_distance && Math.Abs(cst.y - plr.TileY) < Max_distance)) &&
                !nolimit) continue;

            int[] iteminbox = new int[Main.chest.Length];
            for (int indexofiib = 0; indexofiib < iteminbox.Length; indexofiib++) iteminbox[indexofiib] = 0;
            
            for (int j = 0; j < cst.item.Length; j++) {
                if (cst.item[j] == null) continue;
                if (cst.item[j].type == 0) continue;

                var item = cst.item[j];

                string? itemname = Lang.GetItemNameValue(item.type);
                string pyname = Pinyin.GetPinyin(itemname).ToLower().Replace(" ", "");
                string pynamesjm = to_shouzimu(Pinyin.GetPinyin(itemname).ToLower());

                if (pyname.Contains(target.ToLower()) || pynamesjm.Contains(target.ToLower()) || itemname.Contains(target)) {
                    //if (iteminbox[item.type] == 1) continue;

                    iteminbox[item.type] += 1;
                    //plr.SendMessage($"于 {posi_convert(new Vector2(cst.x,cst.y))} ({cst.x},{cst.y}) 处的箱子里检索到 {itemname}",new Color(127, 255, 212));
                    //create_projectile(cst.x,cst.y,args);
                    is_find = true;
                }
            }

            string? finded_name = get_finded_name(iteminbox); 
            if (finded_name != null) {
                plr.SendMessage($"于 {posi_convert(new Vector2(cst.x,cst.y))} ({cst.x},{cst.y}) 处的箱子里检索到 {finded_name}",new Color(127, 255, 212));
                create_projectile(cst.x,cst.y,args);
            }
        }

        if (!is_find) plr.SendInfoMessage("无结果");
    }

    
    private void create_projectile(int x, int y, CommandArgs args)
    {
        try {

            //var random = new Random().Next(167,171);
            var random = 167;
            
            int p = Projectile.NewProjectile(
                Projectile.GetNoneSource(),
                (x * 16 + 16),
                (y * 16 + 16),
                0.0f,
                -8f,
                random,
                0,
                0f
            );
            Main.projectile[p].Kill();
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    private string? get_finded_name(int[] iteminbox)
    {
        string finded_item = "";
        for (int i = 0; i < iteminbox.Length; i++) {
            var itemname = Lang.GetItemNameValue(i);
            if (itemname == null) continue;
            if (iteminbox[i]<=0) continue;
            
            if (finded_item != "") finded_item += ", ";
            finded_item += $"{itemname}*{iteminbox[i]}";
            
        }

        if (finded_item != "") return finded_item;
        return null;
    }
    
    private static string posi_convert(Vector2 Posi)
    {
        int x = (int)Posi.X;
        int y = (int)Posi.Y;
        //int spx = Main.spawnTileX;
        int spx = Main.maxTilesX / 2;
        int spy = (int)Main.worldSurface;
        
        int East_West = (x + 1 - spx)*2;
        int up_down   = (y + 2 - spy)*2;
        string East_West_Direction = East_West > 0 ? "东" : "西";
        string up_down_Direction = up_down > 0 ? "地下" : "地上";

        return $"{East_West_Direction}{Math.Abs(East_West)},{up_down_Direction}{Math.Abs(up_down)}";
    }
    
    private string to_shouzimu(string input)
    {
        return string.Concat(input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(c => char.ToLower(c[0])));
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var asm = Assembly.GetExecutingAssembly();
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
        }
        base.Dispose(disposing);
    }
}