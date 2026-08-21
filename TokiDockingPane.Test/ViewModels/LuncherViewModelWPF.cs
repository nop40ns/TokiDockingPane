using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokiDockingPane.Test.ViewModels;

public class LuncherViewModelWPF
{
    public string ID { get; set; } = "";

    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public LuncherViewModelWPF(string title)
    {
        Title = title;
        Text = title;
    }

}
