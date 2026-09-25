using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows.Controls;
using System.Windows;
using ReactiveUI;
using System.Threading.Tasks;

namespace Wabbajack;

public partial class BrowserWindow : ReactiveUserControl<BrowserWindowViewModel>
{
    public BrowserWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                WebViewWarning.Visibility = Visibility.Collapsed;
                await Task.Delay(TimeSpan.FromSeconds(2));
                WebViewWarning.Visibility = Visibility.Visible;
            });

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => vm is ManualDownloadHandler manual
                    ? manual.WhenAnyValue(m => m.AutoDownloadAvailable)
                    : Observable.Return(false))
                .Switch()
                .Select(available => available ? Visibility.Visible : Visibility.Collapsed)
                .BindToStrict(this, v => v.AutoDownloadButton.Visibility)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => vm is ManualDownloadHandler manual
                    ? manual.WhenAnyValue(m => m.AutoDownloadEnabled)
                    : Observable.Return(false))
                .Switch()
                .Select(enabled => enabled ? "Auto Download: ON" : "Auto Download: OFF")
                .BindToStrict(this, v => v.AutoDownloadButton.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => (vm as ManualDownloadHandler)?.ToggleAutoDownloadCommand)
                .BindToStrict(this, v => v.AutoDownloadButton.Command)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => vm is ManualDownloadHandler manual
                    ? manual.WhenAnyValue(m => m.AutoDownloadStatusText)
                    : Observable.Return(""))
                .Switch()
                .BindToStrict(this, v => v.AutoDownloadStatus.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => vm is ManualDownloadHandler manual
                    ? manual.WhenAnyValue(m => m.AutoDownloadAvailable)
                    : Observable.Return(false))
                .Switch()
                .Select(available => available ? Visibility.Visible : Visibility.Collapsed)
                .BindToStrict(this, v => v.AutoDownloadStatus.Visibility)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel)
                .Select(vm => vm is ManualDownloadHandler manual
                    ? manual.WhenAnyValue(m => m.AutoDownloadNeedsAttention)
                    : Observable.Return(false))
                .Switch()
                .Select(attention => attention ? System.Windows.Media.Brushes.Gold : System.Windows.Media.Brushes.LightGray)
                .BindToStrict(this, v => v.AutoDownloadStatus.Foreground)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BackCommand, v => v.BackButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseCommand, v => v.CloseButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.HeaderText)
                .BindToStrict(this, view => view.Header.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.Instructions)
                .BindToStrict(this, view => view.Instructions.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.Address)
                .BindToStrict(this, view => view.AddressBar.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel.Browser)
                .WhereNotNull()
                .ObserveOnGuiThread()
                .Subscribe(browser =>
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        if (browser.Parent != null)
                        {
                            ((Panel)browser.Parent).Children.Remove(browser);
                        }
                        ViewModel.Browser.Visibility = Visibility.Visible;
                        ViewModel.Browser.Width = double.NaN;
                        ViewModel.Browser.Height = double.NaN;
                        WebViewGrid.Children.Add(browser);
                    });
                })
                .DisposeWith(disposables);
        });
    }
}