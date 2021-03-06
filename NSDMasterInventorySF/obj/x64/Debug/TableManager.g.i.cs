﻿#pragma checksum "..\..\..\TableManager.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "654D3F638B99C3C64F10869576EC6FF5D0E869FE"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace NSDMasterInventorySF {
    
    
    /// <summary>
    /// SheetManager
    /// </summary>
    public partial class SheetManager : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\..\TableManager.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ChangePrefabButton;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\TableManager.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Add;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\..\TableManager.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RemoveSheetButton;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\..\TableManager.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox SheetListBox;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/NSDMasterInventorySF;component/tablemanager.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\TableManager.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 7 "..\..\..\TableManager.xaml"
            ((NSDMasterInventorySF.SheetManager)(target)).Closed += new System.EventHandler(this.SheetManager_OnClosed);
            
            #line default
            #line hidden
            return;
            case 2:
            this.ChangePrefabButton = ((System.Windows.Controls.Button)(target));
            
            #line 10 "..\..\..\TableManager.xaml"
            this.ChangePrefabButton.Click += new System.Windows.RoutedEventHandler(this.ChangePrefabButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 3:
            this.Add = ((System.Windows.Controls.Button)(target));
            
            #line 11 "..\..\..\TableManager.xaml"
            this.Add.Click += new System.Windows.RoutedEventHandler(this.OnNewButtonClick);
            
            #line default
            #line hidden
            return;
            case 4:
            this.RemoveSheetButton = ((System.Windows.Controls.Button)(target));
            
            #line 12 "..\..\..\TableManager.xaml"
            this.RemoveSheetButton.Click += new System.Windows.RoutedEventHandler(this.RemoveSheetButton_OnClick);
            
            #line default
            #line hidden
            return;
            case 5:
            this.SheetListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 15 "..\..\..\TableManager.xaml"
            this.SheetListBox.KeyDown += new System.Windows.Input.KeyEventHandler(this.SheetListBox_OnKeyDown);
            
            #line default
            #line hidden
            
            #line 16 "..\..\..\TableManager.xaml"
            this.SheetListBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.SheetListBox_OnSelectionChanged);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

