﻿using BlazorVirtualGridComponent.businessLayer;
using BlazorVirtualGridComponent.classes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlazorVirtualGridComponent
{
    public class CompCSS : ComponentBase
    {

        [Parameter]
        protected BvgGrid bvgGrid { get; set; }

        bool EnabledRender = true;

        BCss blazorCSS = new BCss();


        protected override Task OnParametersSetAsync()
        {

            EnabledRender = true;

            return base.OnParametersSetAsync();
        }

        protected override bool ShouldRender()
        {
            return EnabledRender;
        }


        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {

            EnabledRender = false;

            base.BuildRenderTree(builder);

            int k = 0;

            // builder.AddMarkupContent(k++,"<style>.my {color:red}");


            CssHelper cssHelper = new CssHelper(bvgGrid.bvgSettings);

            builder.OpenElement(k++, "style");
            builder.AddContent(k++, cssHelper.GetString());
            builder.CloseElement();


            cssHelper = null;

            //builder.OpenElement(k++, "link");
            //builder.AddAttribute(k++, "rel", "stylesheet");
            //builder.AddAttribute(k++, "href", "data:text/css;base64," + cssHelper.GetBase64String());
            //builder.AddAttribute(k++, "type", "text/css");
            //builder.CloseElement();


        }


      


       



    }
}
