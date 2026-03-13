/* Automatic import CSS & JS */
document.addEventListener("DOMContentLoaded", () => {
  const cssFiles = [
    "../../assets/vendor/bootstrap/css/bootstrap.min.css",
    "../../assets/vendor/flexslider/css/flexslider.css",
    "../../assets/vendor/prettyphoto/css/prettyPhoto.css",
    "../../assets/vendor/datepicker/css/datepicker.css",
    "../../assets/vendor/selectordie/css/selectordie.css",
    "https://cdnjs.cloudflare.com/ajax/libs/flag-icons/7.3.2/css/flag-icons.min.css",
    "../../assets/css/main.css",
    "../../assets/css/2035.responsive.css",
  ];

  const jsFiles = [
    "../../assets/vendor/modernizr/modernizr-2.8.3-respond-1.1.0.min.js",
    "../../assets/vendor/jquery/jquery-1.11.1.min.js",
    "../../assets/vendor/bootstrap/js/bootstrap.min.js",
    "../../assets/vendor/retina/retina-1.1.0.min.js",
    "../../assets/vendor/flexslider/js/jquery.flexslider-min.js",
    "../../assets/vendor/superfish/superfish.pack.1.4.1.js",
    "../../assets/vendor/prettyphoto/js/jquery.prettyPhoto.js",
    "../../assets/vendor/datepicker/js/bootstrap-datepicker.js",
    "../../assets/vendor/selectordie/js/selectordie.min.js",
    "../../assets/vendor/slicknav/jquery.slicknav.min.js",
    "../../assets/vendor/parallax/jquery.parallax-1.1.3.js",
    "../../assets/js/auth.js",
  ];

  const importCSS = (href) => {
    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = href;
    document.head.appendChild(link);
  };

  const importJS = (src) => {
    const script = document.createElement("script");
    script.src = src;
    script.async = false;
    document.body.appendChild(script);
  };

  cssFiles.forEach((file) => importCSS(file));

  jsFiles.forEach((file) => importJS(file));
});

/* Active tab */
document.addEventListener("DOMContentLoaded", () => {
  const menuItems = document.querySelectorAll("#navigate > li");

  const currentPath = window.location.pathname.split("/").pop() || "index.html";

  menuItems.forEach((item) => {
    const link = item.querySelector("a");
    if (link) {
      const href = link.getAttribute("href");

      if (href === currentPath) {
        item.classList.add("active");
      } else {
        item.classList.remove("active");
      }
    }
  });
});
