// THIS FILE IS PROVIDED UNDER THE TERMS OF THE CREATIVE COMMONS PUBLIC LICENSE WHICH CAN BE FOUND IN THE PROVIDED License.txt 
// FILE. IT IS PROTECTED BY COPYRIGHT AND/OR OTHER APPLICABLE LAW. ANY USE OF THE WORK OTHER THAN AS AUTHORIZED UNDER ITS 
// LICENSE OR COPYRIGHT LAW IS PROHIBITED.
//
// Written by David-Kay Posmyk (KayPosmyk@gmx.de)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using Common;

using WallpaperManager.Data;

namespace WallpaperManager.Application {
  /// <summary>
  ///   Builds a wallpaper image and implements a special behaviour when building Multiscreen Wallpapers by using 
  ///   <see cref="CreateMultiscreenFromMultiple" />.
  /// </summary>
  /// <inheritdoc select='seealso' />
  /// <seealso cref="WallpaperBuilderBase">WallpaperBuilderBase Class</seealso>
  /// <threadsafety static="true" instance="false" />
  public class WallpaperBuilderOneByOne: WallpaperBuilderBase {
    #region Property: RequiredWallpapersByScreen
    /// <inheritdoc />
    public override ReadOnlyCollection<Int32> RequiredWallpapersByScreen {
      get {
        Int32[] requiredWallpapersByScreen = new Int32[this.ScreensSettings.Count];

        // Check whether we can use the last layout for the next cycle.
        if (this.LastScreenLayout.Count == this.ScreensSettings.Count) {
          requiredWallpapersByScreen[this.LastChangedScreenIndex] = 1;
        } else {
          for (Int32 i = 0; i < this.ScreensSettings.Count; i++) {
            // We want to use a random Wallpaper if random cycling is requested or if a Static Wallpaper should be used but its
            // cycle conditions don't match.
            if (
              (this.ScreensSettings[i].CycleRandomly) || (!this.ScreensSettings[i].StaticWallpaper.EvaluateCycleConditions())
            ) {
              requiredWallpapersByScreen[i] = 1;
            }
          }
        }

        return new ReadOnlyCollection<Int32>(requiredWallpapersByScreen);
      }
    }
    #endregion

    #region Property: LastChangedScreenIndex
    /// <summary>
    ///   <inheritdoc cref="LastChangedScreenIndex" select='../value/node()' />
    /// </summary>
    private Byte lastChangedScreenIndex;

    /// <summary>
    ///   Gets or sets the zero-based index of the last screen changed.
    /// </summary>
    /// <value>
    ///   The zero-based index of the last screen changed.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   Attempted to set a value which is not between <c>0</c> and 
    ///   <see cref="ScreenSettings" /><c>.Count</c> - 1.
    /// </exception>
    protected Byte LastChangedScreenIndex {
      get { return this.lastChangedScreenIndex; }
      set {
        if (!value.IsBetween(0, this.ScreensSettings.Count - 1)) {
          throw new ArgumentOutOfRangeException(ExceptionMessages.GetValueOutOfRange(
            null, value, 
            0.ToString(CultureInfo.CurrentCulture), (this.ScreensSettings.Count - 1).ToString(CultureInfo.CurrentCulture)
          ));
        }

        this.lastChangedScreenIndex = value;
      }
    }
    #endregion

    #region Property: LastScreenLayout
    /// <summary>
    ///   <inheritdoc cref="LastScreenLayout" select='../value/node()' />
    /// </summary>
    private readonly List<Wallpaper> lastScreenLayout = new List<Wallpaper>();

    /// <summary>
    ///   Gets the <see cref="Wallpaper" /> instances used for the last build.
    /// </summary>
    /// <value>
    ///   The collection of <see cref="Wallpaper" /> instances used for the last build.
    /// </value>
    protected List<Wallpaper> LastScreenLayout {
      get { return this.lastScreenLayout; }
    }
    #endregion


    #region Methods: Constructor, CreateMultiscreenFromMultiple
    /// <summary>
    ///   Initializes a new instance of the <see cref="WallpaperBuilderOneByOne"/> class.
    /// </summary>
    /// <inheritdoc />
    public WallpaperBuilderOneByOne(ScreenSettingsCollection screensSettings): base(screensSettings) {
      this.lastScreenLayout = new List<Wallpaper>(Screen.AllScreens.Length);
    }

    /// <summary>
    ///   Creates a multiscreen wallpaper from one or multiple <see cref="Wallpaper" /> objects where one 
    ///   <see cref="Wallpaper" /> is required for each screen on the first call and exactly one <see cref="Wallpaper" /> is
    ///   required on further calls.
    /// </summary>
    /// <inheritdoc />
    public override Image CreateMultiscreenFromMultiple(
      IList<IList<Wallpaper>> wallpapers, Single scaleFactor, Boolean useWindowsFix
    ) {
      if (wallpapers == null) {
        throw new ArgumentNullException(ExceptionMessages.GetVariableCanNotBeNull("wallpapers"));
      }
      if (wallpapers.Count == 0) {
        throw new ArgumentOutOfRangeException(ExceptionMessages.GetCollectionIsEmpty("wallpapers"));
      }
      if (wallpapers.Contains(null)) {
        throw new ArgumentException(ExceptionMessages.GetCollectionContainsNullItem("wallpapers"));
      }

      // This is the collection of Wallpapers which is finally given to the generator method.
      // Note that the order of the Wallpapers in this collection has to be equal with the screen order.
      List<Wallpaper> usedWallpapers = this.LastScreenLayout;

      // Check if we can use the last layout and if so, change just one of the Wallpapers in it.
      if ((this.LastScreenLayout.Count == this.ScreensSettings.Count) && (!this.ScreensSettings.AllStatic)) {
        // We start searching for a screen which requires a random wallpaper from the last screen cycled + 1.
        Int32 currentScreenIndex = this.LastChangedScreenIndex + 1;

        // Find a screen which requests a random Wallpaper.
        for (Int32 i = 0; i < this.ScreensSettings.Count; i++) {
          // Did we reach the last screen?
          if (currentScreenIndex >= this.ScreensSettings.Count) {
            currentScreenIndex = 0;
          }

          // We want to use a random Wallpaper if random cycling is requested or if a Static Wallpaper should be used but its
          // cycle conditions don't match.
          if (
            this.ScreensSettings[currentScreenIndex].CycleRandomly || 
            !this.ScreensSettings[currentScreenIndex].StaticWallpaper.EvaluateCycleConditions()
          ) {
            this.LastChangedScreenIndex = (Byte)currentScreenIndex;
            break;
          }

          currentScreenIndex++;
        }

        // Change the screen which requires a random Wallpaper in the layout.
        this.LastScreenLayout[this.LastChangedScreenIndex] = wallpapers[this.LastChangedScreenIndex][0];

        // Regenerate the changed screen layout.
        Image wallpaper = this.CreateMultiscreenFromMultipleInternal(this.LastScreenLayout, scaleFactor, useWindowsFix);

        return wallpaper;
      }

      // The old layout is not useable or maybe this is the first use of this builder, so we need to generate a new layout.
      this.LastScreenLayout.Clear();
        
      // Loop through all screen settings and use the defined static wallpaper if necessary.
      for (Int32 i = 0; i < this.ScreensSettings.Count; i++) {
        // We use a random Wallpaper if random cycling is requested or if a Static Wallpaper should be used but its
        // cycle conditions don't match.
        if ((this.ScreensSettings[i].CycleRandomly) || (!this.ScreensSettings[i].StaticWallpaper.EvaluateCycleConditions())) {
          usedWallpapers.Add(wallpapers[i][0]);
        } else {
          usedWallpapers.Add(this.ScreensSettings[i].StaticWallpaper);
        }
      }

      // By setting the last index to the last screen, we make sure that the next call of this method will change the wallpaper 
      // of the first screen.
      this.LastChangedScreenIndex = (Byte)(this.ScreensSettings.Count - 1);

      // Generate the wallpaper.
      return this.CreateMultiscreenFromMultipleInternal(usedWallpapers, scaleFactor, useWindowsFix);
    }
    #endregion
  }
}