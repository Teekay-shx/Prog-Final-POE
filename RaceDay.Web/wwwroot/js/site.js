// RaceDay front-end helpers. Kept deliberately small: Bootstrap handles layout and components.

// Ask before destructive actions: add data-confirm="Are you sure?" to a <form>.
document.addEventListener("submit", function (event) {
    var message = event.target.getAttribute && event.target.getAttribute("data-confirm");
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

// Show a preview of a chosen image: <input type="file" data-preview="#imgId">.
document.addEventListener("change", function (event) {
    var input = event.target;
    var selector = input.getAttribute && input.getAttribute("data-preview");
    if (!selector || !input.files || input.files.length === 0) {
        return;
    }

    var target = document.querySelector(selector);
    var file = input.files[0];
    if (target && file.type.indexOf("image/") === 0) {
        target.src = URL.createObjectURL(file);
        target.classList.remove("d-none");
        document.querySelectorAll("[data-hide-on-preview]").forEach(function (element) {
            element.classList.add("d-none");
        });
    }
});
