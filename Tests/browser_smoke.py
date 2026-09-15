"""Browser smoke check using Firefox WebDriver BiDi (requires Python websockets).

Start the app at :5087 and a separate Firefox profile with --remote-debugging-port 9222.
Run: python3 Tests/browser_smoke.py
"""
import asyncio
import json
import time
import sys
import subprocess
import tempfile
from pathlib import Path
import websockets

BASE = "http://127.0.0.1:5088" if "--server" in sys.argv else "http://127.0.0.1:5087"


async def main():
    port = 9223 if "--launch" in sys.argv else 9222
    async with websockets.connect(f"ws://127.0.0.1:{port}/session", proxy=None) as ws:
        sequence = 0

        async def command(method, params):
            nonlocal sequence
            sequence += 1
            await ws.send(json.dumps({"id": sequence, "method": method, "params": params}))
            while True:
                message = json.loads(await ws.recv())
                if message.get("id") == sequence:
                    if message.get("type") == "error":
                        raise RuntimeError(message)
                    return message["result"]

        await command("session.new", {"capabilities": {}})
        context = (await command("browsingContext.create", {"type": "tab"}))["context"]

        async def evaluate(expression):
            result = await command("script.evaluate", {"expression": expression, "target": {"context": context}, "awaitPromise": True})
            if result.get("type") == "exception":
                raise RuntimeError(result)
            return result["result"].get("value")

        async def until(expression, timeout=15):
            end = time.monotonic() + timeout
            while time.monotonic() < end:
                if await evaluate(expression):
                    return
                await asyncio.sleep(.2)
            details = await evaluate("JSON.stringify({url:location.href, body:document.body.innerText, hasToken:!!localStorage.getItem('jwt_token'), scripts:[...document.scripts].map(s=>s.src)})")
            await command("session.end", {})
            raise AssertionError(f"Timed out: {expression}; page=" + str(details))

        async def navigate(path):
            await command("browsingContext.navigate", {"context": context, "url": BASE + path, "wait": "complete"})

        await navigate("/")
        await until("document.getElementById('loginBtn') !== null")
        await evaluate("document.getElementById('loginBtn').click()")
        await until("document.getElementById('authDialog').open")
        username = f"browser_{int(time.time())}"
        await evaluate(f"document.querySelector('[name=username]').value={json.dumps(username)}; document.querySelector('[name=password]').value='test-password'; document.querySelector('[value=register]').click()")
        await until("location.pathname.includes('room-list') && document.getElementById('profileName')?.textContent.length > 0")
        await until("document.getElementById('roomTheme').options.length === 5 && document.getElementById('connectionStatus').classList.contains('hidden')")
        print("PASS Browser registration and room list", flush=True)
        assert await evaluate("fetch('/api/v1/rooms').then(r=>r.status === 401)")
        assert await evaluate("(() => {const token=localStorage.getItem('jwt_token');const p=JSON.parse(atob(token.split('.')[1]));return Math.abs(p.exp-Date.now()/1000-7200)<30})()")
        await evaluate("(async()=>{const canvas=document.createElement('canvas');canvas.width=8;canvas.height=8;const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/png'));const transfer=new DataTransfer();transfer.items.add(new File([blob],'avatar.png',{type:'image/png'}));const input=document.getElementById('avatarFile');input.files=transfer.files;input.dispatchEvent(new Event('change'))})()")
        await until("!document.getElementById('myAvatar').classList.contains('hidden') && document.getElementById('myAvatar').naturalWidth === 8")
        assert await evaluate("(async()=>{const f=new FormData();f.append('file',new File(['<svg>not an allowed image</svg>'],'x.svg',{type:'image/svg+xml'}));return (await fetch('/api/v1/profile/avatar',{method:'POST',headers:{Authorization:'Bearer '+localStorage.getItem('jwt_token')},body:f})).status === 400})()")
        print("PASS Avatar upload/display, invalid file rejection, authentication and two-hour expiry", flush=True)
        await evaluate("document.getElementById('createBtn').click(); document.querySelector('[name=name]').value='Browser <b>room</b>'; document.querySelector('[name=rounds]').value='1'; document.querySelector('[name=turnSeconds]').value='15'; const privateBox=document.querySelector('[name=isPrivate]');privateBox.checked=true;privateBox.dispatchEvent(new Event('change'));document.querySelector('#createForm [name=password]').value='room-secret';document.querySelector('#createForm button').click()")
        await until("location.pathname.includes('/room/') && document.getElementById('roomName')?.textContent === 'Browser <b>room</b>'")
        await until("!document.getElementById('addBotBtn').disabled")
        assert await evaluate("(async()=>{const headers={Authorization:'Bearer '+localStorage.getItem('jwt_token')};const state=await fetch('/api/v1/rooms/my-room/state',{headers}).then(r=>r.json());if(JSON.stringify(state).includes('room-secret'))return false;const response=await fetch('/api/v1/auth/register',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:'guest_'+Date.now(),password:'guest-password'})});const guest=await response.text();const guestHeaders={Authorization:'Bearer '+guest,'Content-Type':'application/json'};const wrong=await fetch('/api/v1/rooms/'+state.id+'/enter',{method:'POST',headers:guestHeaders,body:JSON.stringify({password:'wrong'})});const right=await fetch('/api/v1/rooms/'+state.id+'/enter',{method:'POST',headers:guestHeaders,body:JSON.stringify({password:'room-secret'})});const leave=await fetch('/api/v1/rooms/my-room/leave',{method:'POST',headers:guestHeaders});return wrong.status===403 && right.ok && leave.ok})()")
        print("PASS Private room passwords, membership and leave API", flush=True)
        for i in range(4):
            await evaluate("document.getElementById('addBotBtn').click()")
            await until(f"document.querySelectorAll('.player').length === {i+2}")
        await evaluate("document.getElementById('readyBtn').click()")
        await until("!document.getElementById('startGameBtn').disabled")
        await evaluate("document.getElementById('readyBtn').click()")
        await until("document.getElementById('startGameBtn').disabled")
        print("PASS Browser room creation, bots and readiness toggle; text is escaped")
        await command("browsingContext.setViewport", {"context": context, "viewport": {"width": 375, "height": 812}, "devicePixelRatio": 1})
        assert await evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile horizontal overflow"
        print("PASS 375px mobile room layout")
        await evaluate("document.getElementById('readyBtn').click()")
        await until("!document.getElementById('startGameBtn').disabled")
        await evaluate("document.getElementById('startGameBtn').click()")
        await until("!document.getElementById('gameInfo').classList.contains('hidden')")
        await until("!document.getElementById('chatInput').disabled", timeout=20)
        await evaluate("document.getElementById('chatInput').value='<img src=x onerror=alert(1)> clue'; document.getElementById('sendBtn').click()")
        await until("location.pathname.includes('voting')", timeout=30)
        await until("document.querySelectorAll('.vote-card').length === 5")
        assert await evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile voting horizontal overflow"
        await evaluate("document.querySelector('.vote-card:not(:disabled)').click()")
        await until("document.querySelectorAll('.vote-card.selected').length === 1")
        await navigate("/voting/")
        await until("document.querySelectorAll('.vote-card.selected').length === 1")
        print("PASS Complete browser round, mobile voting and vote restoration")
        await evaluate("document.getElementById('finishVoteBtn').click()")
        await until("document.getElementById('resultDialog').open", timeout=20)
        assert await evaluate("document.getElementById('resultMessage').textContent.includes('Слово:')")
        await evaluate("document.getElementById('resultClose').click()")
        await until("location.pathname.includes('/room/') && document.getElementById('readyBtn')?.textContent === 'Готов'")
        print("PASS Result modal and return to same lobby")
        # Go to the list without leaving the room, then verify persisted stats.
        await navigate("/room-list/")
        await until("document.getElementById('profileName')?.textContent.length > 0")
        await evaluate("document.getElementById('historyBtn').click()")
        await until("document.getElementById('historyDialog').open")
        assert await evaluate("document.getElementById('statistics').textContent.includes('Партий: 1')")
        print("PASS Browser statistics and history")
        await command("browsingContext.close", {"context": context})
        await command("session.end", {})


if "--launch" in sys.argv:
    with tempfile.TemporaryDirectory(prefix="spy-firefox-") as profile:
        server = None
        if "--server" in sys.argv:
            root = Path(__file__).resolve().parent.parent
            server = subprocess.Popen(["dotnet", str(root / "Web/bin/Debug/net10.0/Web.dll"), "--contentRoot", str(root / "Web"), "--urls", BASE], cwd=profile, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        browser = subprocess.Popen(["firefox", "--headless", "--no-remote", "--profile", profile, "--remote-debugging-port", "9223", "about:blank"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            time.sleep(2)
            asyncio.run(main())
        finally:
            browser.terminate()
            browser.wait(timeout=15)
            if server:
                server.terminate()
                server.wait(timeout=15)
else:
    asyncio.run(main())
