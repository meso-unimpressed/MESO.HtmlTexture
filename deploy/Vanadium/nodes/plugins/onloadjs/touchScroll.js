//const { Scrollbar } = window

class TouchScrollingMeta
{
	constructor(element) {

		this.deltaTime = 0;
		this.velocityX = 0;
		this.velocityY = 0;
		this.delayedVelocityX = 0;
		this.delayedVelocityY = 0;
		this.isTouchScrolling = true;
		this.damping = 3.5; // seconds
		this.cancelScroll = false;

		this.domElement = element;
		this.prevScrollX = this.getScroll().x;
		this.prevScrollY = this.getScroll().y;
		this.prevTimeStamp = performance.now();
		
		// this.domElement.addEventListener('wheel', (event => {
		// 	if(!this.cancelScroll)
		// 	{
		// 		event.preventDefault();
		// 		this.velocityX = this.delayedVelocityX = this.velocityY = this.delayedVelocityY = 0;
		// 		this.cancelScroll = true;
		// 	}
		// 	//delete this.domElement.touchScrollMeta;
		// }).bind(this));
		
		this.domElement.addEventListener('touchstart', ((event) => {
			this.prevScrollX = this.getScroll().x;
			this.prevScrollY = this.getScroll().y;
			this.cancelScroll = false;
			this.isTouchScrolling = true;
		}).bind(this));

		this.domElement.addEventListener('touchend', ((event) =>{
			this.isTouchScrolling = false;
		}).bind(this));
		this.domElement.addEventListener('touchcancel', ((event) => {
			this.isTouchScrolling = false;
		}).bind(this));
		
		window.requestAnimationFrame(this.mainloop.bind(this));
	}

	onTouchEnd()
	{
		this.velocityX = Math.max(this.velocityX, this.delayedVelocityX);
		this.velocityY = Math.max(this.velocityY, this.delayedVelocityY);
		this.isTouchScrolling = false;
	}

	mainloop(timeStamp) {
		this.deltaTime = Math.abs(timeStamp - this.prevTimeStamp);
		this.prevTimeStamp = timeStamp;

		if(this.isTouchScrolling) {
			this.onScroll(this.getScroll().x, this.getScroll().y);
		} else {
			if(Math.abs(this.velocityX) >= 1 || Math.abs(this.velocityY) >= 1)
			{
				var delta = (this.deltaTime / 1000) / (this.damping / 6);
				var damp = Math.max(0, 1-(delta));
				this.velocityX *= damp;
				this.velocityY *= damp;
				this.scrollBy(this.velocityX, this.velocityY);
			}
			else
			{
				this.velocityX = this.delayedVelocityX = this.velocityY = this.delayedVelocityY = 0;
			}
		}
		window.requestAnimationFrame(this.mainloop.bind(this));
	}

	isRootElement()
	{
		return this.domElement === document.body ||
			this.domElement === document ||
			this.domElement === window;
	}

	getScroll()
	{
		if(this.isRootElement()) return {
			x: window.scrollX,
			y: window.scrollY
		}
		else return {
			x: this.domElement.scrollLeft,
			y: this.domElement.scrollTop
		}
	}

	scrollBy(scrollX, scrollY)
	{
		if(this.isRootElement()) window.scrollBy(scrollX, scrollY);
		else this.domElement.scrollBy(scrollX, scrollY);
	}

	onScroll(scrollX, scrollY) {
		if(!this.isTouchScrolling) return;

		var dx = scrollX - this.prevScrollX;
		var dy = scrollY - this.prevScrollY;
		this.velocityX = dx;
		this.velocityY = dy;

		window.setTimeout((() => {
			this.delayedVelocityX = dx;
			this.delayedVelocityY = dy;
		}).bind(this, dx, dy), 80);

		this.prevScrollX = scrollX;
		this.prevScrollY = scrollY;
	}
}

var touched = false;

window.addEventListener('touchstart', event => touched = true);
window.addEventListener('touchend', event => touched = false);
window.addEventListener('touchcancel', event => touched = false);

window.addEventListener('scroll', event => {
	if(!("touchScrollMeta" in event.target) && touched)
	{
		var t = event.target;
		if(event.target === window || event.target === document) t = document.body;
		event.target.touchScrollMeta = new TouchScrollingMeta(event.target);
		console.log(event.target);
	}
}, true);
