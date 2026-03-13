(function ($) {
  "use strict";

  /* Mobile Detect */
  var isMobile = {
    Android: function () {
      return navigator.userAgent.match(/Android/i);
    },
    BlackBerry: function () {
      return navigator.userAgent.match(/BlackBerry/i);
    },
    iOS: function () {
      return navigator.userAgent.match(/iPhone|iPad|iPod/i);
    },
    Opera: function () {
      return navigator.userAgent.match(/Opera Mini/i);
    },
    Windows: function () {
      return navigator.userAgent.match(/IEMobile/i);
    },
    any: function () {
      return (
        isMobile.Android() ||
        isMobile.BlackBerry() ||
        isMobile.iOS() ||
        isMobile.Opera() ||
        isMobile.Windows()
      );
    },
  };

  /* Parallax */
  var Parallax = function () {
    jQuery(".parallax").each(function () {
      var parallaxId = $(this).attr("id");
      $("#" + parallaxId).parallax("50%", 0.4);
    });
  };

  /* Breadcrumb Full Screen */
  var BreadcrumbFullScreen = function () {
    var winHeight = $(window).height();
    $("#under-construction-page").css({ height: winHeight });
    $("#home").css({ height: winHeight });
    var consTop = (winHeight - 390) / 2;
    $(".under-cons-top").css({ paddingTop: consTop });
  };

  /* Tabs */
  var Tabs = function () {
    $(".panel-style a").click(function () {
      $(".panel-luxen").find(".panel-style").removeClass("active");
      $(".panel-luxen").removeClass("active-panel");
      $(".panel-luxen")
        .find(".plus-box")
        .html('<i class="fa fa-angle-down"></i>');
      $(this).parent().parent().addClass("active");
      $(this).parent().parent().parent().addClass("active-panel");
      $(this).parent().find(".plus-box").html('<i class="fa fa-angle-up"></i>');
    });

    $(".tabbed-area a").click(function (e) {
      e.preventDefault();
      $(this).tab("show");
      $(".tabbed-area").find(".active-tab").removeClass("active-tab");
      $(this).parent(".about-destination-box").addClass("active-tab");
    });
  };

  var luxenContactForm = function () {
    $(function () {
      $("#ajax-contact-form").submit(function (e) {
        e.preventDefault();
        var cname = $("#cname").val();
        var csubject = $("#csubject").val();
        var cmail = $("#cmail").val();
        var ctext = $("#ctext").val();
        if (
          $.trim(cname) == "" ||
          $.trim(csubject) == "" ||
          $.trim(cmail) == "" ||
          $.trim(ctext) == ""
        ) {
          if (cname == "") {
            $(this).find("#cname").addClass("fixed-error");
          }
          if (csubject == "") {
            $(this).find("#csubject").addClass("fixed-error");
          }
          if (cmail == "") {
            $(this).find("#cmail").addClass("fixed-error");
          }
          if (ctext == "") {
            $(this).find("#ctext").addClass("fixed-error");
          }
        } else {
          jQuery.ajax({
            type: "POST",
            url: "mail.php",
            data: $("#ajax-contact-form").serialize(),
            error: function () {
              $(".contact-form").html("Error!");
            }, //Hata veri
            success: function (veri) {
              $(".contact-form").html(veri);
            }, //Başarılı
          });
        }
      });
    });

    $("#ajax-contact-form").on("keypress", ".fixed-error", function (e) {
      $(this).removeClass("fixed-error");
    });
  };

  var AjaxReservation = function () {
    $("#res-submit").click(function (e) {
      e.preventDefault();
      var fxname = $("#dpd1").val();
      var fxmail = $("#dpd2").val();
      if ($.trim(fxname) == "" || $.trim(fxmail) == "") {
        if (fxname == "") {
          $("#ajax-reservation-form").find("#dpd1").addClass("fixed-error");
        }
        if (fxmail == "") {
          $("#ajax-reservation-form").find("#dpd2").addClass("fixed-error");
        }
        $("#ajax-reservation-form").find("#res-submit").addClass("shake");
      } else {
        jQuery.ajax({
          type: "POST",
          url: "reservation.php?reservationstep=one",
          data: $("#ajax-reservation-form").serialize(),
          error: function () {
            $(".reserve-form-area").html("Error!");
          },
          success: function (content) {
            $(".reserve-form-area").html(content);
          },
          complete: function () {
            $("#ajax-reservation-send").on(
              "keypress",
              ".fixed-error",
              function (e) {
                $(this).removeClass("fixed-error");
              }
            );

            $("#res-send").click(function (e) {
              e.preventDefault();
              var snname = $("#name").val();
              var snsurname = $("#surname").val();
              var snphone = $("#phone").val();
              var snmail = $("#mail").val();
              if (
                $.trim(snname) == "" ||
                $.trim(snsurname) == "" ||
                $.trim(snphone) == "" ||
                $.trim(snmail) == ""
              ) {
                if (snname == "") {
                  $("#ajax-reservation-send")
                    .find("#name")
                    .addClass("fixed-error");
                }
                if (snsurname == "") {
                  $("#ajax-reservation-send")
                    .find("#surname")
                    .addClass("fixed-error");
                }
                if (snphone == "") {
                  $("#ajax-reservation-send")
                    .find("#phone")
                    .addClass("fixed-error");
                }
                if (snmail == "") {
                  $("#ajax-reservation-send")
                    .find("#mail")
                    .addClass("fixed-error");
                }
              } else {
                jQuery.ajax({
                  type: "POST",
                  url: "reservation.php?reservationstep=two",
                  data: $("#ajax-reservation-send").serialize(),
                  error: function () {
                    $(".reserve-form-area").html("Error!");
                  },
                  success: function (content) {
                    $(".reserve-form-area").html(content);
                  },
                });
              }
            });
          },
        });
      }
    });

    $("#ajax-reservation-form").on("keypress", ".fixed-error", function (e) {
      $(this).removeClass("fixed-error");
    });
  };

  $(document).ready(function () {
    BreadcrumbFullScreen();
    Tabs();
    Parallax();
    luxenContactForm();
    AjaxReservation();

    /* Mobile Menu */
    $(".navigate").slicknav();
    /* Mobile Menu */

    /* PrettyPhoto */
    $("a[rel^='prettyPhoto']").prettyPhoto({
      animation_speed: "fast" /* fast/slow/normal */,
      slideshow: 5000 /* false OR interval time in ms */,
      theme: "light_square",
      social_tools: false,
    });

    /* SelectOrDie */
    if (jQuery(".pretty-select").length) {
      $(".pretty-select").selectOrDie();
    }

    /* Fitvids */
    if (jQuery(".fitvids").length) {
      jQuery(".fitvids").fitVids();
    }

    /* Flexslider */
    $(".flexslider-thumb").flexslider({
      animation: "slide",
      animationLoop: false,
      prevText: "",
      nextText: "",
      controlNav: "thumbnails",
    });

    /* Superfish */
    if (jQuery("#navigate").length) {
      $("#navigate")
        .superfish({
          delay: 0,
          animation: { opacity: "show", height: "show" },
          speed: "normal",
        })
        .supposition();
    }

    /* Google Maps */
    if (jQuery(".maps").length) {
      var map;
      map = new GMaps({
        el: "#map",
        lat: -12.043333,
        lng: -77.028333,
        zoomControl: true,
        zoomControlOpt: {
          style: "SMALL",
          position: "TOP_LEFT",
        },
        panControl: false,
        streetViewControl: false,
        mapTypeControl: false,
        overviewMapControl: false,
      });
    }

    /* Weather */
    if (jQuery("#weather").length) {
      var html;
      $.simpleWeather({
        location: "London",
        woeid: "",
        unit: "c",
        success: function (weather) {
          html = "<h6>" + weather.city + "</h6>";
          html += '<div class="clearfix">';
          html +=
            '<div class="pull-left"><i class="icon-' +
            weather.code +
            '"></i></div>';
          html +=
            '<div class="pull-left"><h3>' +
            weather.temp +
            "&deg;" +
            weather.units.temp +
            "</h3><h3>" +
            weather.currently +
            "</h3></div>";
          html += "</div>";

          $("#weather").html(html);
        },
        error: function (error) {
          $("#weather").html("<p>" + error + "</p>");
        },
      });
    }

    /* TimeBox */
    if (jQuery("#dpd1").length) {
      var nowTemp = new Date();
      var now = new Date(
        nowTemp.getFullYear(),
        nowTemp.getMonth(),
        nowTemp.getDate(),
        0,
        0,
        0,
        0
      );

      var checkin = $("#dpd1")
        .datepicker({
          onRender: function (date) {
            return date.valueOf() < now.valueOf() ? "disabled" : "";
          },
        })
        .on("changeDate", function (ev) {
          if (ev.date.valueOf() > checkout.date.valueOf()) {
            var newDate = new Date(ev.date);
            newDate.setDate(newDate.getDate() + 1);
            checkout.setValue(newDate);
          }
          checkin.hide();
          $("#dpd2")[0].focus();
        })
        .data("datepicker");
      var checkout = $("#dpd2")
        .datepicker({
          onRender: function (date) {
            return date.valueOf() <= checkin.date.valueOf() ? "disabled" : "";
          },
        })
        .on("changeDate", function (ev) {
          checkout.hide();
        })
        .data("datepicker");
    }
  });
  $(window).load(function () {
    $(".flexslider").flexslider({
      animation: "slide",
      animationLoop: false,
      prevText: "",
      nextText: "",
      start: function (slider) {
        $(".flexslider").removeClass("slider-loading");
      },
    });

    /* Isotope */
    var $container = $(".portfolio-box");
    var $filter = $(".portfolio-filters");
    $container.isotope({
      filter: "*",
      layoutMode: "sloppyMasonry",
      animationOptions: { duration: 400 },
    });
    $filter.find("a").click(function () {
      var selector = $(this).attr("data-filter");
      $filter.find("a").removeClass("active");
      $(this).addClass("active");
      $container.isotope({
        filter: selector,
        animationOptions: {
          animationDuration: 400,
          queue: false,
        },
      });
      return false;
    });
    /* Isotope */
  });
})(jQuery);
