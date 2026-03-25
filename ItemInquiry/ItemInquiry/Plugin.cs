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
    public override Version Version => new Version(1, 0);

    public Plugin(Main game) : base(game) {
    }

    private string helpmsg = "使用/ii <item_name> 以检索物品.";
    
    public override void Initialize() {
        Commands.ChatCommands.Add(new Command("iteminquiry", OnCommand,"ii"));
    }

    private void OnCommand(CommandArgs args)
    {
        if(args.Parameters.Count == 0) {
            args.Player.SendInfoMessage(this.helpmsg);
        }

        else if (args.Parameters.Count == 2) {
            if (args.Parameters[1].ToLower() == "nolimit" || args.Parameters[1].ToLower() == "nl") {
                chest_finder(args.Parameters[0],args.Player,true,args);
            }
            else {
                args.Player.SendInfoMessage(helpmsg);
            }
        }

        else if (args.Parameters.Count == 1) {
            chest_finder(args.Parameters[0],args.Player,false,args);
        }
        else {
            args.Player.SendInfoMessage(this.helpmsg);
        }
    }

    private void chest_finder(string target,TSPlayer plr, bool nolimit,CommandArgs args)
    {
        //var pyname = Pinyin.GetPinyin(itemname);
        var chests = Main.chest;
        bool is_find = false;
        for (int i = 0; i < chests.Length; i++) {
            var cst = chests[i];
            
            if (cst == null) continue;
            if ((!(Math.Abs(cst.x - plr.TileX) < 100 && Math.Abs(cst.y - plr.TileY) < 100)) && !nolimit)continue;
            
            int[] finded = new int[Main.chest.Length];
            
            for (int j = 0; j < cst.item.Length; j++) {
                if (cst.item[j] == null) continue;
                if (cst.item[j].type == 0) continue;
                
                var item = cst.item[j];
                string? itemname = Lang.GetItemNameValue(item.type);
                var pyname = Pinyin.GetPinyin(itemname).ToLower().Replace(" ","");
                var pynamesjm = get_shouzimu(Pinyin.GetPinyin(itemname).ToLower());
                
                if (pyname.Contains(target.ToLower())||pynamesjm.Contains(target.ToLower())) {
                    if (finded[item.type] == 1) continue;
                    finded[item.type] = 1;
                    plr.SendMessage($"于 {posi_convert(new Vector2(cst.x,cst.y))} ({cst.x},{cst.y}) 处的箱子里检索到 {itemname}",new Color(127, 255, 212));
                    create_projectile(cst.x,cst.y,args);
                    
                    is_find = true;
                }
            }
        }
        if(!is_find) plr.SendInfoMessage("无结果");
    }

    private void create_projectile(int x,int y,CommandArgs args)
    {
        try {
            
            //int p = Projectile.NewProjectile(
            //     Projectile.GetNoneSource(),         
            //     (x * 16 + 8),          
            //     (y * 16 + 8),     
            //     0.0f,                                 
            //     -8f,                               
            //     167,                       
            //     0,                                  
            //     0.0f
            // );
            //Main.projectile[p].Kill();
            
            //var posi = new Vector2(x * 16 + 8,y * 16 + 8);
            var posi = new Vector2(x, y);
            var col = new Color(127, 255, 212);
            var dust = Dust.NewDustDirect(posi, 64, 64, 267, 0.1f, -5f, 0, col,5);
            Dust.UpdateDust();
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    
    public static string posi_convert(Vector2 Posi)
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
    
    private string get_shouzimu(string input)
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