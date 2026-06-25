import { Component, Injector } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { ContractKolDto, ContractKolStatus, ReceiveStatus } from '@shared/service-proxies/service-proxies';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Tag } from 'primeng/tag';
import { Button } from 'primeng/button';

@Component({
    templateUrl: './view-contract-kol-detail-dialog.component.html',
    standalone: true,
    imports: [
        AbpModalHeaderComponent,
        LocalizePipe,
        DatePipe,
        DecimalPipe,
        Tag,
        Button,
    ],
})
export class ViewContractKolDetailDialogComponent extends AppComponentBase {
    record: ContractKolDto = new ContractKolDto();
    kolName: string = '—';
    contractName: string = '—';

    readonly statusLabels: Record<number, string> = {
        [ContractKolStatus.Register]: 'Đăng ký',
        [ContractKolStatus.Approve]: 'Duyệt đăng ký',
        [ContractKolStatus.Processing]: 'Đang tiến hành',
        [ContractKolStatus.MktOk]: 'Mkt duyệt',
        [ContractKolStatus.DpmOk]: 'Quản lý duyệt',
        [ContractKolStatus.OnAir]: 'Đã air',
        [ContractKolStatus.Following]: 'Theo dõi',
        [ContractKolStatus.Paid]: 'Đã thanh toán',
        [ContractKolStatus.Done]: 'Hoàn thành',
        [ContractKolStatus.Cancel]: 'Hủy',
        [ContractKolStatus.Reject]: 'Từ chối',
    };

    readonly statusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
        [ContractKolStatus.Register]: 'secondary',
        [ContractKolStatus.Approve]: 'info',
        [ContractKolStatus.Processing]: 'info',
        [ContractKolStatus.MktOk]: 'warn',
        [ContractKolStatus.DpmOk]: 'warn',
        [ContractKolStatus.OnAir]: 'success',
        [ContractKolStatus.Following]: 'secondary',
        [ContractKolStatus.Paid]: 'success',
        [ContractKolStatus.Done]: 'success',
        [ContractKolStatus.Cancel]: 'danger',
        [ContractKolStatus.Reject]: 'danger',
    };

    readonly receiveStatusLabels: Record<number, string> = {
        [ReceiveStatus.NotShip]: 'Chưa gửi',
        [ReceiveStatus.Shipping]: 'Đang gửi',
        [ReceiveStatus.Received]: 'Đã nhận',
        [ReceiveStatus.NotReceived]: 'Không nhận',
    };

    readonly receiveStatusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'danger'> = {
        [ReceiveStatus.NotShip]: 'secondary',
        [ReceiveStatus.Shipping]: 'info',
        [ReceiveStatus.Received]: 'success',
        [ReceiveStatus.NotReceived]: 'danger',
    };

    constructor(injector: Injector, public bsModalRef: BsModalRef) {
        super(injector);
    }

    getStatusLabel(status: ContractKolStatus | undefined): string {
        return status != null ? (this.statusLabels[status] ?? '') : '—';
    }

    getStatusSeverity(status: ContractKolStatus | undefined): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        return status != null ? (this.statusSeverities[status] ?? 'secondary') : 'secondary';
    }

    getReceiveStatusLabel(s: ReceiveStatus | undefined): string {
        return s != null ? (this.receiveStatusLabels[s] ?? '') : '—';
    }

    getReceiveStatusSeverity(s: ReceiveStatus | undefined): 'secondary' | 'info' | 'success' | 'danger' {
        return s != null ? (this.receiveStatusSeverities[s] ?? 'secondary') : 'secondary';
    }
}
