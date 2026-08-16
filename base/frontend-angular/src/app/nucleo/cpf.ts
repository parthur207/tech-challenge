export function cpfValido(cpf: string): boolean {

    const valor=(cpf ?? '').trim();

    if(!/^\d{11}$/.test(valor)) {
        return false;
    }

    if(new Set(valor).size === 1) {
        return false;
    }

    const numeros= valor.split('').map(Number);

    const calcularDigito = (quantidade: number, pesoInicial: number) : number => {
        let soma = 0;
        for (let i = 0; i < quantidade; i++) {
            soma += numeros[i] * (pesoInicial - i);
        }
        const resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    };

    if(numeros[9] !== calcularDigito(9, 10)) {
        return false;
    }
    
    return numeros[10] === calcularDigito(10, 11);  
}