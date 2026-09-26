async function getMyId(){
    const url = '/me';
    const response = await fetch(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });
    if (response.ok){
        let json = await response.json();
        window.myId = json.id;
        window.myNickname = json.username;
    }
}