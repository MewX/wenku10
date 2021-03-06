﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;

using Net.Astropenguin.Linq;

using GR.Effects;
using GR.Model.ListItem;

namespace wenku10.Scenes
{
	sealed partial class HyperBanner
	{
		public Uri CoverUri { get; private set; }

		public HorizontalAlignment Align { get; set; } = HorizontalAlignment.Left;

		public ICanvasBrush RingBrush;
		public string RingText;
		public int SideLen = 196;

		public float MarginFactor = 0.65f;
		public float IrisFactor = 0.65f;
		public float TextRotation = 0;
		public float TextSpeed = 0.002f * ( float ) Math.PI;
		private Size StageSize;
		private Size EyeBox;

		private Rect FillRect;

		private BookBannerItem BindItem;

		private CanvasBitmap CoverBmp;
		private ICanvasBrush CoverBrush;
		private CanvasSolidColorBrush TextBrush;

		Action<CanvasDrawingSession> _RippleDraw;

		private void InitRipple( ActiveItem Context )
		{
			_RippleDraw = DrawNothing;
			RingText = Context.Name;
			// RingText = "The quick Brown Fox Jumps Over the Lazy dog";

			if ( Context is BookBannerItem )
			{
				BindItem = ( BookBannerItem ) Context;

				if( BindItem.BannerExist )
				{
					CoverUri = BindItem.UriSource;
				}

				BindItem.PropertyChanged += BindItem_PropertyChanged;
			}
		}

		private void ActivateRippleDraw( Action<CanvasDrawingSession> F )
		{
			if ( F == DrawClickedRipple )
			{
				// Inner R for image from 0 -> MaxR
				ImgR_t = MaxR;
				ImgRi_c = 0;
				ImgRi_t = MaxR;

				// Inner R for Ring from ImgR_c -> -MaxR
				RingRi_t = RingR_t = -MaxR;
				RingRi_c = ImgR_c;
			}

			_RippleDraw = F;
		}

		private void BindItem_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "BannerExist" )
			{
				CoverUri = BindItem.UriSource;
				var j = ReloadCover();
			}
		}

		Vector2 PCenter;

		float MaxR;
		float ImgR_c;
		float ImgR_t;
		float ImgRi_c;
		float ImgRi_t;

		float RingR_c;
		float RingR_t;
		float RingRi_c;
		float RingRi_t;

		float TextR_c;
		float TextR_t;
		Color TextBrush_t;

		float BlockHeight;
		float[] TextWidths;
		CanvasTextLayout TextLayout;
		CircleTextRenderer TextRenderer;

		bool Clicked = false;

		private void DrawIdleRipple( CanvasDrawingSession ds )
		{
			Easings.ParamTween( ref ImgR_c, ImgR_t, 0.80f, 0.20f );
			Easings.ParamTween( ref RingR_c, RingR_t, 0.75f, 0.25f );
			Easings.ParamTween( ref TextR_c, TextR_t, 0.80f, 0.20f );

			// TextBrush Color
			TextBrush.Color = Easings.ParamTween( TextBrush.Color, TextBrush_t, 0.80f, 0.20f );

			float RingW = ( RingR_c - ImgR_c );
			float RingR = ImgR_c + 0.5f * RingW;

			// +2 to fill in the edge between the image and the ring
			ds.DrawCircle( PCenter, RingR, RingBrush, RingW + 2 );

			TextRenderer.PrepareDraw( ds, TextR_c, TextRotation += TextSpeed );
			TextLayout.DrawToTextRenderer( TextRenderer, PCenter );

			ds.FillCircle( PCenter, ImgR_c, CoverBrush );
		}

		private void DrawClickedRipple( CanvasDrawingSession ds )
		{
			Easings.ParamTween( ref ImgR_c, ImgR_t, 0.75f, 0.25f );
			Easings.ParamTween( ref ImgRi_c, ImgRi_t, 0.75f, 0.25f );
			Easings.ParamTween( ref RingR_c, RingR_t, 0.875f, 0.125f );
			Easings.ParamTween( ref RingRi_c, RingRi_t, 0.875f, 0.125f );
			float RingW = ( RingR_c - RingRi_c );
			float RingR = RingRi_c + 0.5f * RingW;

			float ImgRW_c = ( ImgR_c - ImgRi_c );
			float ImgRR_c = ImgRi_c + 0.5f * ImgRW_c;

			ICanvasBrush RBrush = 0 < RingR ? RingBrush : CoverBrush;

			ds.DrawCircle( PCenter, RingR, RBrush, RingW );
			ds.DrawCircle( PCenter, ImgRR_c, CoverBrush, ImgRW_c );
		}

		private void UpdateText( ICanvasResourceCreator Device )
		{
			CanvasTextFormat Format = new CanvasTextFormat() { FontFamily = "Segoe UI" };
			TextLayout = new CanvasTextLayout( Device, RingText, Format, float.PositiveInfinity, 0 );
			BlockHeight = ( float ) TextLayout.DrawBounds.Height;
			TextWidths = TextLayout.ClusterMetrics.Remap( x => x.Width );
		}

		private async Task ReloadCover()
		{
			if ( ResCreator == null || CoverUri == null ) return;
			CoverBmp = await CanvasBitmap.LoadAsync( ResCreator, CoverUri );

			FitCover();
		}

		private void FitCover()
		{
			if ( StageSize.IsZero() || CoverBmp == null ) return;

			// RippleEx
			EyeBox = new Size( SideLen, SideLen );

			Rect EyeRect;
			(EyeRect, FillRect) = ImageUtils.FitImage( EyeBox, CoverBmp );

			float SW = ( float ) EyeRect.Width;
			float ImgMargin = SW * ( 1 - MarginFactor );

			SW = SW - ImgMargin;

			float Offset = ImgMargin * 0.5f;
			float Scale = SW / ( float ) FillRect.Width;

			MaxR = SW * 0.5f;

			float Px = Offset + MaxR;
			float Py = Px;

			if ( Align == HorizontalAlignment.Right )
			{
				Px = ( float ) StageSize.Width - Px;
			}
			else if ( Align == HorizontalAlignment.Center )
			{
				Px = 0.5f * ( float ) StageSize.Width;
			}

			Blur();

			PCenter = new Vector2( Px, Py );
			TextRenderer = new CircleTextRenderer( PCenter, TextWidths, TextBrush );

			CoverBrush = new CanvasImageBrush( ResCreator, CoverBmp )
			{
				SourceRectangle = FillRect
				, Transform = Matrix3x2.CreateScale( Vector2.One * Scale, Vector2.Zero ) * Matrix3x2.CreateTranslation( new Vector2( Px - MaxR, Offset ) )
			};
		}
	}
}