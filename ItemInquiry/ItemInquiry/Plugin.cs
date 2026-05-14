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
    public override Version Version => new Version(0, 4);

    public Plugin(Main game) : base(game)
    {
    }

    
    private bool Enable = true;
    private bool Allow_Nolimit = true;
    private int Max_distance = 100;
    private int projectile_id = 167;
    private string helpmsg = "使用/ii <item_name> 以检索物品.";

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command("iteminquiry", OnCommand, "ii"));
    }
    
    private void OnCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0) {
            args.Player.SendInfoMessage(this.helpmsg);
            return;
        }

        if (args.Parameters[0]=="none")args.Parameters[0] = "";

        if (args.Parameters.Count == 2) {
            if (args.Parameters[1] == "nolimit" || args.Parameters[1] == "nl")
                chest_finder_task(args.Parameters[0], args.Player, true, args);
            else args.Player.SendInfoMessage(helpmsg);
            return;
        }

        if (args.Parameters.Count == 1)
            chest_finder_task(args.Parameters[0],args.Player,false,args);
    }

    private void chest_finder(string target, TSPlayer plr, bool nolimit, CommandArgs args)
    {
        try {

            if (!Enable) return;
            if (!Allow_Nolimit && nolimit) {
                args.Player.SendErrorMessage("Nolimit不可用");
                return;
            }

            var chests = Main.chest;
            int box_num = 0;
            int item_num = 0;

            for (int i = 0; i < chests.Length; i++) {
                var cst = chests[i];

                if (cst == null) continue;
                if ((!(Math.Abs(cst.x - plr.TileX) < this.Max_distance &&
                       Math.Abs(cst.y - plr.TileY) < Max_distance)) &&
                    !nolimit) continue;

                int[] iteminbox = new int[Main.chest.Length];
                for (int indexofiib = 0; indexofiib < iteminbox.Length; indexofiib++) iteminbox[indexofiib] = 0;

                for (int j = 0; j < cst.item.Length; j++) {
                    if (cst.item[j] == null) continue;
                    if (cst.item[j].type == 0) continue;
                    var item = cst.item[j];

                    if (is_target(item, target)) {
                        iteminbox[item.type] += cst.item[j].stack;
                        item_num += cst.item[j].stack;
                    }
                }

                if (show_result_msg(iteminbox, cst, plr, box_num)) box_num++;
            }

            if (item_num == 0) plr.SendInfoMessage("无结果");
            else plr.SendInfoMessage($"找到{box_num}个箱子, 共{item_num}个物品.");

        }
        catch (Exception e) {
            Console.WriteLine(e);
            args.Player.SendErrorMessage(e.ToString());
        }
    }

    private void chest_finder_task(string target, TSPlayer plr, bool nolimit, CommandArgs args)
    {
        Task.Run(() => chest_finder(target, plr, nolimit, args));
        
    }

    private void create_projectile(int x, int y)
    {
        int p = Projectile.NewProjectile(
            Projectile.GetNoneSource(),
            (x * 16 + 16),
            (y * 16 + 16),
            0.0f,
            -8f,
            projectile_id,
            0,
            0f
        );
        Main.projectile[p].Kill();
        
        Thread.Sleep(1);
        //Thread.SpinWait(100000);
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

    private bool show_result_msg(int[] iteminbox,Chest cst,TSPlayer plr,int box_num)
    {
        string? finded_name = get_finded_name(iteminbox);
        if (finded_name != null) {
            
            var color = new Color(127, 255, 212);
            if (box_num %2 == 1)color = new Color(135,206,250);

            plr.SendMessage($"> 于 {posi_convert(new Vector2(cst.x, cst.y))} ({cst.x},{cst.y}) 处的箱子里检索到 {finded_name}",
                color);
            create_projectile(cst.x, cst.y);

            return true;
        }
        return false;
    }

    private string posi_convert(Vector2 Posi)
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

    private bool is_target(Item item, string target)
    {
        string itemname = Lang.GetItemNameValue(item.type);
        string pyname = Pinyin.GetPinyin(itemname).ToLower();//Replace(" ", "");
        // string pynamesjm = to_shouzimu(Pinyin.GetPinyin(itemname).ToLower());
        string targetV2U = target.ToLower().Replace("v", "u");
        // return pyname.Contains(target.ToLower()) || pynamesjm.Contains(target.ToLower()) || itemname.Contains(target) || pyname.Contains(targetV2U.ToLower());
        return isMatch(targetV2U, pyname);
    }
    
    private string string_clip(string str, int start, int end)
    {
        return  (str.Substring(start, end - start));
    }
    
    private bool ptr_Alldone(int[] ptr, int max)
    {
        
        for (int i = 0; i < ptr.Length; i++) {
            if (ptr[i] != max) return false;
        }
        return true;
    }

    private bool isMatchOnce(string[] input, string[] str)
    {
        if (input.Length != str.Length) return false;
            
        for (int i = 0; i < input.Length; i++)
        {
            if (!str[i].Contains(input[i])) return false;
        }
        return true;
    }

    private bool isMatch(string input, string str)
    {
        var strlist = str.Split(' ');

        if (strlist.Length == 1)
        {
            return strlist[0].Contains(input) ? true : false;
        }
        
        int[] ptr =  new int[strlist.Length-1];
        for (int i = 0; i < ptr.Length; i++)ptr[i] =0;
            
        string[] inputclip = new string[strlist.Length];
        while (true)
        {
            for (int i = 0; i < strlist.Length; i++)
            {
                if (i == 0) inputclip[i] = string_clip(input, 0, ptr[i]);
                else if (i == strlist.Length - 1) inputclip[i] = string_clip(input, ptr[i-1], input.Length);
                else inputclip[i] = string_clip(input, ptr[i-1], ptr[i]);
            }

            // printstrlist(inputclip);
            // printintlist(ptr);
            if (isMatchOnce(inputclip, strlist)) {
                return true;
            }

            if (ptr_Alldone(ptr, input.Length)) break;
                
            for (int i = ptr.Length-1; i >=0; i--) {
                if (ptr[i] < input.Length) {
                    ptr[i]++;
                        
                    for (int j = i+1; j < ptr.Length; j++) {
                        ptr[j] = ptr[i];
                    }
                    break;
                }
            }
        }
        return false;
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