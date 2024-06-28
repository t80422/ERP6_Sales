// aside
$(function(){
    $(".aside-title").on("click", function(e){
        if($(this).parent().has(".aside-link")) {
            e.preventDefault();
        }
        if(!$(this).hasClass("is-active")) {
            $(".aside-link").slideUp();
            $(".aside-title").removeClass("is-active");
            $(this).next(".aside-link").slideDown();
            $(this).addClass("is-active");
        }
        else if($(this).hasClass("is-active")) {
            $(this).removeClass("is-active");
            $(this).next(".aside-link").slideUp();
        }
    });
});


// popup
$(function(){
    $(".popup-close").on("click", function(e){
        $(".popup-wrap").removeClass("is-active");
        $(".popup-overlay").removeClass("is-active");
    });

    $(".btn-cancel").on("click", function(e){
        $(".popup-wrap").removeClass("is-active");
        $(".popup-overlay").removeClass("is-active");
    });
});



