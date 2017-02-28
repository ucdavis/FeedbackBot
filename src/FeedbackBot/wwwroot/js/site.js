// Write your Javascript code.
document.querySelector('button').addEventListener('click',function clickHandler(e){

    console.log("new button thing")

    this.removeEventListener('click',clickHandler,false);

    e.preventDefault();
    var self = this;
    setTimeout(function(){
        self.className = 'loading';
    },50);

    setTimeout(function(){
        self.className = 'ready';
    },100);

},false);
